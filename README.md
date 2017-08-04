# Cat-Post Processing Effects for Unity 5
Various fast and efficient post-processing effects for Unity

![Main Img 1][ElevatorDoor_IMG]
The main goal is to create fast and efficient post-processing effects for Unity.

There are currently 5 different post-processing effects included:
- Temporal Anti-Alialising (TAA)
- Ambient Occlusion
- Importance Sampled Screen Space Reflections (SSR) with retro reflections and specular elongation
- Chromatic Aberration
- Bloom Effect with energy conservation

## Temporal Anti-Alialising
![TemporalAntiAlialisingGUI_IMG][TemporalAntiAlialisingGUI_IMG]
- **Sharpness:** Artificially sharpens the image. High values can look cheap.
- **Velocity Scale:** Controls how sensitive it reacts when pixels move along the screen.
- **Response:** Controls how fast a pixel responds to a change of color.
- **Tolerance Margin:** 
- **Jitter Matrix:** What jitter sequence should be used to shake the camera. (Halton Sequence recomended)
- **Halton Seq. Length:** The length of the Halton Sequence if selected.

## Ambient Occlusion
![AmbientOcclusionGUI_IMG][AmbientOcclusionGUI_IMG]
- **Intensity:** The intensity of the ambient occlusion.
- **Sample Count:** How many samples should be taken? Try to keep it small.
- **Radius:** The search radius. Try to keep it small, too.
- **Debug On:** Visualizes the generated Ambient Occlusion.


## Screen Space Reflections
For best Results use this effect together with Temporal Anti-Alialising.

Performance comparison, 2Kx2K resolution: | ms / frame
--- | ---
No SSR: | 10.5 - 12.3 ms
SSR from Unitys [Post Processing Stack](https://github.com/Unity-Technologies/PostProcessing): | 23.2 - 24.0 ms
[SSSR by cCharkes](https://github.com/cCharkes/StochasticScreenSpaceReflection): | 21.8 - 22.3 ms
Cat SSR: | 17.0 - 17.8 ms

![ScreenSpaceReflectionsGUI_IMG][ScreenSpaceReflectionsGUI_IMG]

RayTracing
- **Ray Trace Resol.:** The Resolution of the generated hit texture. _High Performance Impact!_
- **Step Count:** The maximum amount of steps the ray tracer will take. _High Performance Impact!_
- **Min Pixel Stride:** The minimum size of a step im pixels.
- **Max Pixel Stride:** The maximum size of a step im pixels.
- **Cull Back Faces:** If selected rays towards camera are ignored. Can Increase performance a lot in indoor scenes
- **Max Refl. Distance:** The radius around the Player (in meters) in which reflections are calculated. mosly usfull in large outdoor scenes

Reflections
- **Reflection Resol.:** The Resolution of the generated reflection texture. _Medium Performance Impact!_
- **Intensity:** The intensity of the reflections.
- **Distance Fade:** Controls how soon reflections are faded out, based on distance to the camera.
- **Ray Length Fade:** Controls how soon reflections are faded out, based on length of ray.
- **Screen Edge Fade:** Controls how soon reflections are faded out, based on distance from the border of the screen.
- **Retro Reflections:** Controls whether retro-reflections are used or not. Very low, if any performance impact.

Importance sampling
- **Sample Count:** Amount of samples per pixel (usually `4`). _Medium Performance Impact!_
- **Bias (Spread):** Controls how far the samples are spread out. Physically correct is a value of `1`, but that can lead to artifacts
- **Use Mip Map:** Mip Maps have a surprisingly low impact on performance. They can de-noise the Reflections substantially, but the can also lead to some subtle artifacts.

Temporal
- **Use Temporal:** Temporally de-noises the importance sampled reflections. But this can currently lead to strong artifacts when an object moves in front of a highly reflective background.
- **Response:** TBD.
- **Tolerance Margin:** TBD.

Debugging
- **Debug On:** TBD.
- **Debug Mode:** TBD.
- **Mip Level For Debug:** TBD.

## Chromatic Aberration
![ChromaticAberrationGUI_IMG][ChromaticAberrationGUI_IMG]

## Bloom
![BloomGUI_IMG][BloomGUI_IMG]
- **Intensity:** The intensity of the bloom.
- **Dirt Intensity:** Only effective when a Dirt Texture is selected  
- **Dirt Texture:** The dirt on the lense. (RGB)
- **Min Luminance:** Minimum luminance required for the bloom to appear.
- **Knee Strength:** 
- **Debug On:** Visualizes the Bloom only.

## Install Intructions
Simply put the "Cat" folder into your "Assets" folder.

![Main Img 2][coloredBalls_IMG]


[coloredBalls_IMG]:              Media/coloredBalls.png               "All Effects in action 1"
[ElevatorDoor_IMG]:              Media/ElevatorDoor.png               "All Effects in action 2"
[TemporalAntiAlialisingGUI_IMG]: Media/CatAAGUI.png                   "Temporal Anti-Alialising GUI"
[AmbientOcclusionGUI_IMG]:       Media/CatAOGUI.png                   "Ambient Occlusion GUI"
[ScreenSpaceReflectionsGUI_IMG]: Media/CatSSRGUI.png                  "Screen Space Reflections GUI"
[ChromaticAberrationGUI_IMG]:    Media/CatChromaticAberrationGUI.png  "Chromatic Aberration GUI"
[BloomGUI_IMG]:                  Media/CatBloomGUI.png                "Bloom Effect GUI"
