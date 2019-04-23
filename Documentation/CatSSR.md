## Screen Space Reflections
For best Results use this effect together with Temporal Anti-Alialising.

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


[ScreenSpaceReflectionsGUI_IMG]:       https://i.imgur.com/QTXvVsY.png                   "Screen Space Reflections GUI"






