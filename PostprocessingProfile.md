Post processing profiles bundle effects settings together so they can be reused and it becomes easier to maintain a consistent look and feel of your game.

### Creating a new Post Processing Profile

Right-click in the Assets tab and select `Create`->`Cat Post processing profile`

![Imgur](https://i.imgur.com/mW9Xf0O.png)

### The anatomy of an effect

The checkboxes on the left of each value you choose which settings will be overwritten by the profile. The checkbox next to the effects doesn't turn effects On or Off, but rather acts like a master switch for overwriting.

![Imgur](https://i.imgur.com/K6BykNZ.png)

Here, for example only the intesity of the ambient oclusion effect is overwritten; as well as some of the bloom setings.

### The Overwrite Order

The profile in the post processing manager of your camera overwrites post processing volumes with a higher `importance` value, which overwrite volumes with a lower `importance` value, which overwrite volumes, that are set to global. 


