.PHONY: debug release test stage package clean

debug:
	msbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Debug

release:
	msbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Release

test:
	msbuild Build.proj /t:Test /p:Mono=true;BuildFlavour=Release

stage:
	msbuild Build.proj /t:Stage /p:Mono=true;BuildFlavour=Release

package:
	msbuild Build.proj /t:Package /p:Mono=true;BuildFlavour=Release

clean:
	msbuild Build.proj /t:Clean /p:Mono=true

