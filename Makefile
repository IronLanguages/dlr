.PHONY: all clean ironpython

all: ironpython

ironpython:
	xbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Debug

ironpython-release:
	xbuild Build.proj /t:Build /p:Mono=true;BuildFlavour=Release

testrunner:
	xbuild Test/TestRunner/TestRunner.sln	

test-ipy: ironpython testrunner
	mono Test/TestRunner/TestRunner/bin/Debug/TestRunner.exe Test/IronPython.tests /all

clean:
	xbuild Build.proj /t:Clean /p:Mono=true

