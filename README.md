PerforcePlugin for FlashDevelop
===============================

Build instructions:

1. Obtain FlashDevelop source code: https://github.com/fdorg/flashdevelop
2. Compile solution
2. Add Perforce.csproj to FlashDevelop.sln
3. Go to project properties for Perforce project:
4. Select __Build__ tab -> set __Output Path__ to ```bin\Debug\``` for _Debug_ and ```bin\Release\``` for _Release_
5. Select __Build Events__ tab -> set __Post-build event commandline__ to ```copy "$(ProjectDir)bin\$(ConfigurationName)\Perforce.dll" "$(SolutionDir)\FlashDevelop\Bin\Debug\Plugins"```
6. Select __Debug__ tab -> set __Start external program__ to the path of the FlashDevelop exe. Usually found in ```FlashDevelop\Bin\Debug\FlashDevelop.exe```
7. Set Perforce project as __Startup Project__ and compile!

Enjoy!
