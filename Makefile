.PHONY: debug release test stage package clean

debug:
	msbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Debug /verbosity:minimal /nologo

release:
	msbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Release /verbosity:minimal /nologo

test:
	msbuild Build.proj /t:Test /p:Mono=true;BuildFlavour=Release /verbosity:minimal /nologo

stage:
	msbuild Build.proj /t:Stage /p:Mono=true;BuildFlavour=Release /verbosity:minimal /nologo

package:
	msbuild Build.proj /t:Package /p:Mono=true;BuildFlavour=Release /verbosity:minimal /nologo

clean:
	msbuild Build.proj /t:Clean /p:Mono=true

