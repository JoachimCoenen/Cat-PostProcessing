
## Post Processing Volumes

&&ADD IMAGE&&

Post processing volumes allow to specify different effect settings for different areas, e.g. Inside a building and outside of it or a different visual mood around a graveyard etc. 
They only affect the space _inside_  the associated colliders as well as a small ~~border~~ `FIND A BETTER WORD!!` 
  around around it in order to ease the TRANSITION WHEN entering and exiting the volume. 
To associate a collider with your post processing volume you simply add it to the `GameObject`  or any  child object. 

&&ADD IMAGE&&


<!--For this you've got post processing volumes
Add an empty GameObject to your scene, add the PostProcessing Volume script and a collider of your choice to it. It will be the area of effect. Pro tip: you can add multiple colliers as children to your volume. 
The profile assigned to the volume is only applied when the camera is inside one of the colliers? of the post processing volume _and_  has a Cat Post Processing Manager. Be aware that Post Processing Volumes with a higher `Importance` value and the profile set in the post processing manager can overwrite some or all settings./>

