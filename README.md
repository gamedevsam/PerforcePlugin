PerforcePlugin for FlashDevelop
===============================

Build instructions:

1. Obtain FlashDevelop source code (SVN only, sorry): https://code.google.com/p/flashdevelop/source/checkout
2. Compile solution
2. Add Perforce.csproj to FlashDevelop.sln
3. Go to project properties for Perforce project:
4. Select __Build__ tab -> set __Output Path__ to ```bin\Debug\``` for Debug and ```bin\Release\``` for release
5. Setelct __Build Events__ tab -> set __Post-build event commandline__ to ```copy "$(ProjectDir)bin\$(ConfigurationName)\Perforce.dll" "$(SolutionDir)\FlashDevelop\Bin\Debug\Plugins"```
