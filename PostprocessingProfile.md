Post processing profiles bundle effects settings together so they can be reused and it becomes easier to maintain a consistent look and feel of your game.

### Creating a new Post Processing Profile

Right-click in the Assets tab and select `Create`->`Cat Post processing profile`

&&& ADD IMAGE &&&

### The anatomy of an effect

![Imgur](https://i.imgur.com/eHf4ygk.png)


With the checkboxes on the left of each value you choose which settings to override. The checkbox next to the effects does not turn effects On or Off, but rather acts like a master switch for overriding.

&&& BETTER / MORE EXPLANATION &&&


### The Override Order

The profile in the post processing manager of your camera overrides post processing volumes with a higher `importance` value, which override volumes with a lower `importance` value, which override volumes, that are set to global. 



