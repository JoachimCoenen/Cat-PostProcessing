
### Post Processing Volumes

￼

Post processing volumes allow to specify different effect settings for different areas, e.g. Inside a building and outside of it or a different visual mood around a graveyard etc. They only affect the space inside the associated colliders as well as a small border around it in order to ease the transition when entering and exiting the volume. To associate a collider with your post processing volume you simply add it to the `GameObject` or any child object.

￼

- **Shared Profile**: The post processing profile that is applied within the volume.

- **Blend Distance**: Distance around volume used for blending.

- **Importance**: The degree of “importance” of this volume compared to its neighbours. Higher values indicate greater importance; more important volumes will be applied over less important ones in cases where an object is within range of two or more volumes.

