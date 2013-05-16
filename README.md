PerforcePlugin for FlashDevelop
===============================

Build instructions:

1. Obtain FlashDevelop source code (SVN only, sorry): https://code.google.com/p/flashdevelop/source/checkout
2. Compile solution
2. Add Perforce.csproj to FlashDevelop.sln
3. Go to project properties for Perforce project:
4. Select "Build" tab -> set "OutputPath" to "bin\Debug\" for Debug and "bin\Release\" for release
5. Setelct "Build Events" tab -> set "Post-build event commandline" to ```copy "$(ProjectDir)bin\$(ConfigurationName)\Perforce.dll" "$(SolutionDir)\FlashDevelop\Bin\Debug\Plugins"```
