# VRCFT2CVR
Natively integrated VRCFaceTracking in ChilloutVR

## Setup

> [!IMPORTANT]
> 
> You **MUST** have MelonLoader version [v0.6.2>=](https://github.com/LavaGang/MelonLoader/releases/latest)

1. Download the [Latest Release](https://github.com/200Tigersbloxed/VRCFT2CVR/releases/latest/download/VRCFT2CVR.Plugin.dll)
2. Copy the `VRCFT2CVR.Plugin.dll` into your `Plugins` directory
3. Install any module(s) to `/path/to/ChilloutVR/VRCFTModules`

> [!CAUTION]
> 
> All VRCFT Modules have to be recompiled to support TigersUniverse's [VRCFaceTracking](https://github.com/TigersUniverse/VRCFaceTracking) with `net481` support!
> 
> You can find a list of precompiled modules [here](https://github.com/TigersUniverse/VRCFaceTracking?tab=readme-ov-file#-external-modules)

## Configuration

> [!TIP]
> 
> You can install [BTKUILib](https://github.com/BTK-Development/BTKUILib) to manage config options in-game; however, some options may require a game restart.

### Integrated Tracking Support

> [!WARNING]  
>
> Integrated Tracking Support is experimental and may not work correctly

Integrated Tracking Support allows VRCFT modules to interact with ChilloutVR's built-in Face Tracking component and built-in Eye Tracking Integration.

Default: **false**

> [!NOTE]  
>
> You must have the following options disabled:
>  + ImplementationVRViveFaceTracking
>  + ImplementationDesktopViveFaceTracking
>  + ImplementationVRTobiiEyeTracking
>  + ImplementationVRTobiiEyeBlinking
>  + ImplementationDesktopTobiiEyeTracking
>  + ImplementationDesktopTobiiEyeBlinking
>
> All necessary options will be automatically configured by the mod at runtime

### Use Binary Parameters

Use Binary Parameters enables the usage of Binary Parameters with VRCFaceTracking modules.

Default: **true**

## Building

Before building, you must build [TigersUniverse/VRCFaceTracking](https://github.com/TigersUniverse/VRCFaceTracking) because this mod relies on dependencies from there.

The building process is split into the two projects.

**VRCFT2CVR**

The actual mod which hooks into the game and edits game values to manage and apply face tracking.

> [!CAUTION]
> 
> **DO NOT INSTALL THE MOD**
> 
> The mod will be loaded at runtime by the plugin.

**VRCFT2CVR.Plugin**

The plugin which loads the mod and all of its dependencies at runtime.

> [!CAUTION]
> 
> **THIS IS A PLUGIN**
>
> This will go into your MelonLoader's Plugins folder!

### VRCFT2CVR

To build the mod, simply create a `VRCFT2CVR.csproj.user` file with the following information

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <!-- This is the ChilloutVR game directory -->
        <ChilloutVRDirectory>path\to\ChilloutVR\</ChilloutVRDirectory>
        <!-- This is the build directory for TigersUniverse's VRCFaceTracking -->
        <VRCFaceTrackingBuild>path\to\VRCFaceTracking\VRCFaceTracking.Core\bin\Release\net481\</VRCFaceTrackingBuild>
    </PropertyGroup>
</Project>
```

then build!

### VRCFT2CVR.Plugin

To build the plugin which loads the mod, simple create a `VRCFT2CVR.Plugin.csproj.user` file with the following information

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- This is the ChilloutVR game directory -->
    <ChilloutVRDirectory>path\to\ChilloutVR\</ChilloutVRDirectory>
    <!-- This is the build directory for TigersUniverse's VRCFaceTracking -->
    <VRCFaceTrackingBuild>path\to\VRCFaceTracking\VRCFaceTracking.Core\bin\Release\net481\</VRCFaceTrackingBuild>
    <!-- This is the file for System.ComponentModel.DataAnnotations.dll, a dependency of VRCFaceTracking -->
    <DataAnnotationsFile>C:\\Windows\\Microsoft.NET\\assembly\\GAC_MSIL\\System.ComponentModel.DataAnnotations\\v4.0_4.0.0.0__31bf3856ad364e35\\System.ComponentModel.DataAnnotations.dll</DataAnnotationsFile>
  </PropertyGroup>
</Project>
```

then build! Be sure to copy the `VRCFT2CVR.Plugin.dll` to your Plugins folder!

## Credits

+ [Hypernex.Unity](https://github.com/TigersUniverse/Hypernex.Unity)
  + Wrote code for everything VRCFT Parameters
+ [VRCFaceTracking (Unity)](https://github.com/TigersUniverse/VRCFaceTracking)
+ [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking)