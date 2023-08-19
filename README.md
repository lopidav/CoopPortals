# mod-template (testing)

How to set up:
1. Copy the files somewhere
2. Set up the mod name in the following places:
    - Mod.csproj (Project > PropertyGroup > AssemblyName)
    - manifest.json (id, name)
    - Mod.cs (namespace, class)
3. Configure the paths in Mod.csproj.user:
    - kaartdorp - points to the unity project root (NOT the repo root)
    - unity - points to the installation of Unity 2020.3.6f1
4. Configure the build script, specifically the variables under `----- CONFIGURE THESE -----`
5. Develop your mod :D
6. Run the build script to build your mod and automatically copy all the files it needs