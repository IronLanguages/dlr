#!/usr/bin/env pwsh
[CmdletBinding()]
Param(
    [Parameter(Position=1)]
    [String] $target = "build",
    [String] $configuration = "Release",
    [String[]] $frameworks=@('net462','net8.0','net9.0','net10.0'),
    [String] $platform = $null,  # auto-detect
    [switch] $runIgnored
)

$ErrorActionPreference="Continue"

[int] $global:Result = 0

$_BASEDIR = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

$_defaultFrameworkSettings = @{
    "runner" = "dotnet";
    "tests" = @{ "Microsoft.Dynamic.Test" = "tests/Microsoft.Dynamic.Test"; "Microsoft.Scripting.Test" = "tests/Microsoft.Scripting.Test"; "Metadata" = "tests/Metadata"; "HostingTest" = "tests/HostingTest" };
    "args" = @('test', '__BASEDIR__/__TESTFOLDER__', '-f', '__FRAMEWORK__', '-o', '__BASEDIR__/bin/__CONFIGURATION__/__FRAMEWORK__', '-c', '__CONFIGURATION__', '--no-build', '-l', "trx;LogFileName=__FILTERNAME__-__TESTDESC__-__FRAMEWORK__-__CONFIGURATION__-result.trx", '-s', '__RUNSETTINGS__');
    "filterArg" = '--filter="__FILTER__"';
    "filters" = @{
        "all" = "";
    }
}

# Overrides for the default framework settings
$_FRAMEWORKS = @{}

function Main([String] $target, [String] $configuration) {
    dotnet msbuild Build.proj /m /t:$target /p:Configuration=$configuration /verbosity:minimal /nologo /p:Platform="Any CPU" /bl:build-$target-$configuration.binlog
    # use the exit code of msbuild as the exit code for this script
    $global:Result = $LastExitCode
}

function GenerateRunSettings([String] $folder, [String] $framework, [String] $platform, [String] $configuration, [bool] $runIgnored) {
    [System.Xml.XmlDocument]$doc = New-Object System.Xml.XmlDocument

#   <RunSettings>
#     <RunConfiguration>
#       <TargetPlatform>x64</TargetPlatform> <!-- if defined -->
#     </RunConfiguration>
#     <TestRunParameters>
#       <Parameter name="FRAMEWORK" value="net462" />
#       <Parameter name="CONFIGURATION" value="Release" />
#     </TestRunParameters>
#   </RunSettings>

    $dec = $doc.CreateXmlDeclaration("1.0","UTF-8",$null)
    $doc.AppendChild($dec) | Out-Null

    $runSettings = $doc.CreateElement("RunSettings")

    $runConfiguration = $doc.CreateElement("RunConfiguration")
    $runSettings.AppendChild($runConfiguration) | Out-Null
    if ($platform) {
        $targetPlatform = $doc.CreateElement("TargetPlatform")
        $targetPlatform.InnerText = $platform
        $runConfiguration.AppendChild($targetPlatform) | Out-Null
    }

    $testRunParameters = $doc.CreateElement("TestRunParameters")
    $runSettings.AppendChild($testRunParameters) | Out-Null

    $parameter = $doc.CreateElement("Parameter")
    $parameter.SetAttribute("name", "FRAMEWORK")
    $parameter.SetAttribute("value", $framework)
    $testRunParameters.AppendChild($parameter) | Out-Null

    $parameter = $doc.CreateElement("Parameter")
    $parameter.SetAttribute("name", "CONFIGURATION")
    $parameter.SetAttribute("value", $configuration)
    $testRunParameters.AppendChild($parameter) | Out-Null

    if($runIgnored) {
        $parameter = $doc.CreateElement("Parameter")
        $parameter.SetAttribute("name", "RUN_IGNORED")
        $parameter.SetAttribute("value", "true")
        $testRunParameters.AppendChild($parameter) | Out-Null
    }

    $doc.AppendChild($runSettings) | Out-Null

    $fileName = [System.IO.Path]::Combine($_BASEDIR, $folder, "runsettings.xml")
    $doc.Save($fileName)
    return $fileName
}

function Test([String] $target, [String] $configuration, [String[]] $frameworks, [String] $platform) {
    foreach ($framework in $frameworks) {
        $frameworkSettings = $_FRAMEWORKS[$framework]
        if ($null -eq $frameworkSettings) { $frameworkSettings = $_defaultFrameworkSettings }

        foreach ($testdesc in $frameworkSettings["tests"].Keys) {
            if ($testdesc -eq "HostingTest" -and ($framework -ne "net462" -or -not $IsWindows)) {
                continue
            }

            $testname = "";
            $filtername = $target

            # generate the runsettings file for the settings
            $runSettings = GenerateRunSettings $frameworkSettings["tests"][$testdesc] $framework $platform $configuration $runIgnored

            if(!$frameworkSettings["filters"].ContainsKey($target)) {
                Write-Warning "No tests available for '$target' trying to run single test '$framework.$target'"
                $testname = "$framework.$target"
                $filtername = "single"
            }

            $filter = $frameworkSettings["filters"][$filtername]

            $replacements = @{
                "__FRAMEWORK__" = $framework;
                "__CONFIGURATION__" = $configuration;
                "__FILTERNAME__" = $filtername;
                "__FILTER__" = $filter;
                "__BASEDIR__" = $_BASEDIR;
                "__TESTNAME__" = $testname;
                "__TESTFOLDER__" = $frameworkSettings["tests"][$testdesc];
                "__TESTDESC__" = $testdesc;
                "__RUNSETTINGS__" = $runSettings;
            };

            $runner = $frameworkSettings["runner"]
            # make a copy of the args array
            [Object[]] $args = @() + $frameworkSettings["args"]
            # replace the placeholders with actual values
            for([int] $i = 0; $i -lt $args.Length; $i++) {
                foreach($r in $replacements.Keys) {
                    $args[$i] = $args[$i].Replace($r, $replacements[$r])
                }
            }

            if($filter.Length -gt 0) {
                $tmp = $frameworkSettings["filterArg"].Replace('__FILTER__', $replacements['__FILTER__'])
                foreach($r in $replacements.Keys) {
                    $tmp = $tmp.Replace($r, $replacements[$r])
                }
                $filter = $tmp
            }

            Write-Host "$runner $args $filter"

            # execute the tests
            & $runner $args $filter

            # save off the status in case of failure
            if($LastExitCode -ne 0) {
                $global:Result = $LastExitCode
            }
        }
    }
}

function Purge() {
    Main "Clean" "Release"
    Main "Clean" "Debug"

    Write-Verbose "Deleting ""obj"" directories..."
    Get-ChildItem -Name "obj" -Directory -Path $_BASEDIR -Recurse | Remove-Item -Force -Recurse

    Write-Verbose "Deleting ""bin"" directories..."
    foreach ($dir in @("", "src/samples/sympl/csharp", "tests/Microsoft.Dynamic.Benchmarks")) {
        if (Test-Path (Join-Path $_BASEDIR $dir "bin" -OutVariable targetPath)) {
            Remove-Item -Path $targetPath -Force -Recurse
        }
    }

    Write-Verbose "Deleting "".binlog"" files..."
    Remove-Item -Path (Join-Path $_BASEDIR "*.binlog")

    Write-Verbose "Deleting packaging artifacts..."
    foreach ($dir in @("Release", "Debug")) {
        if (Test-Path (Join-Path $_BASEDIR "dist" $dir -OutVariable targetPath)) {
            Remove-Item -Path $targetPath -Force -Recurse
        }
    }

    Write-Verbose "Deleting test run settings..."
    Get-ChildItem -Filter "runsettings*.xml" -Path (Join-Path $_BASEDIR "tests") -Recurse | Remove-Item

    Write-Information "Done. Consider restoring dependencies." -InformationAction Continue
}

switch -wildcard ($target) {
    # debug targets
    "restore-debug" { Main "RestoreReferences" "Debug" }
    "debug"         { Main "Build" "Debug" }
    "clean-debug"   { Main "Clean" "Debug" }
    "stage-debug"   { Main "Stage" "Debug" }
    "package-debug" { Main "Package" "Debug" }
    "test-debug-*"  { Test $target.Substring(11) "Debug" $frameworks $platform; break }
    "test-debug"    { Test "all" "Debug" $frameworks $platform; break }

    # release targets
    "release"       { Main "Build" "Release" }

    # general targets
    "restore"       { Main "RestoreReferences" $configuration }
    "build"         { Main "Build" $configuration }
    "clean"         { Main "Clean" $configuration }
    "stage"         { Main "Stage" $configuration }
    "package"       { Main "Package" $configuration }
    "test-*"        { Test $target.Substring(5) $configuration $frameworks $platform; break }
    "test"          { Test "all" $configuration $frameworks $platform; break }

    # utility targets
    "purge"         { Purge }

    default { Write-Error "No target '$target'" ; Exit -1 }
}

Exit $global:Result
