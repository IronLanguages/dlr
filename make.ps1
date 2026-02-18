#!/usr/bin/env pwsh
[CmdletBinding()]
Param(
    [Parameter(Position=1)]
    [String] $target = "release",
    [String] $configuration = "Release",
    [String[]] $frameworks=@('net462','net8.0','net9.0','net10.0'),
    [String] $platform = $null,  # auto-detect
    [switch] $runIgnored
)

[int] $global:Result = 0
[bool] $global:isUnix = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix

$_BASEDIR = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

if(!$global:isUnix) {
    $_VSWHERE = [System.IO.Path]::Combine(${env:ProgramFiles(x86)}, 'Microsoft Visual Studio\Installer\vswhere.exe')
    $_VSINSTPATH = ''

    if([System.IO.File]::Exists($_VSWHERE)) {
        $_VSINSTPATH = & "$_VSWHERE" -latest -requires Microsoft.Component.MSBuild -property installationPath
    } else {
        Write-Error "Visual Studio 2022 17.14.26 or later is required"
        Exit 1
    }

    if(-not [System.IO.Directory]::Exists($_VSINSTPATH)) {
        Write-Error "Could not determine installation path to Visual Studio"
        Exit 1
    }

    if([System.IO.File]::Exists([System.IO.Path]::Combine($_VSINSTPATH, 'MSBuild\Current\Bin\MSBuild.exe'))) {
        $_MSBUILDPATH = [System.IO.Path]::Combine($_VSINSTPATH, 'MSBuild\Current\Bin\')
        if ($env:PATH -split ';' -notcontains $_MSBUILDPATH) {
            $env:PATH = [String]::Join(';', $env:PATH, $_MSBUILDPATH)
        }
    }
}

$_defaultFrameworkSettings = @{
    "runner" = "dotnet";
    "tests" = @{ "Microsoft.Dynamic.Test" = "Tests/Microsoft.Dynamic.Test"; "Microsoft.Scripting.Test" = "Tests/Microsoft.Scripting.Test"; "Metadata" = "Tests/Metadata" };
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

switch -wildcard ($target) {
    # debug targets
    "restore-debug" { Main "RestoreReferences" "Debug" }
    "debug"         { Main "Build" "Debug" }
    "clean-debug"   { Main "Clean" "Debug" }
    "stage-debug"   { Main "Stage" "Debug" }
    "package-debug" { Main "Package" "Debug" }
    "test-debug-*"  { Test $target.Substring(11) "Debug" $frameworks $platform; break }

    # release targets
    "restore"       { Main "RestoreReferences" "Release" }
    "release"       { Main "Build" "Release" }
    "clean"         { Main "Clean" "Release" }
    "stage"         { Main "Stage" "Release" }
    "package"       { Main "Package" "Release" }
    "test-*"        { Test $target.Substring(5) "Release" $frameworks $platform; break }

    default { Write-Error "No target '$target'" ; Exit -1 }
}

Exit $global:Result
