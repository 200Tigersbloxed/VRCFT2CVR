# VRCFT2CVR
Natively integrated VRCFaceTracking in ChilloutVR

## Setup

> [!IMPORTANT]
> 
> You **MUST** have MelonLoader version [v0.6.2>=](https://github.com/LavaGang/MelonLoader/releases/latest)

1. Download the [Latest Release](https://github.com/200Tigersbloxed/VRCFT2CVR/releases/latest/download/VRCFT2CVR.zip)
2. Copy all the folders from the release artifact to `/path/to/ChilloutVR`
  + Folder `UserLibs` contains all libraries that need to be loaded in order for VRCFT2CVR to work
  + Folder `Mods` contains the mod dll
    + Will also include the `.pdb` if it's a debug build
3. Install any module(s) to `/path/to/ChilloutVR/VRCFTModules`

> [!CAUTION]
> 
> All VRCFT Modules have to be recompiled to support TigersUniverse's [VRCFaceTracking](https://github.com/TigersUniverse/VRCFaceTracking) with `net481` support!
> 
> You can find a list of precompiled modules [here](https://github.com/TigersUniverse/VRCFaceTracking?tab=readme-ov-file#-external-modules)

## Configuration

> [!TIP]
> 
> You can install [BTKUILib](https://github.com/BTK-Development/BTKUILib) (and optionally with [CommonBTKUI](https://github.com/dakyneko/DakyModsCVR/tree/master/CommonBTKUI)) to manage config options in-game; however, some options may require a game restart.

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

## Credits

+ [Hypernex.Unity](https://github.com/TigersUniverse/Hypernex.Unity)
  + Wrote code for everything VRCFT Parameters
+ [VRCFaceTracking (Unity)](https://github.com/TigersUniverse/VRCFaceTracking)
+ [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking)