# Asset Brush
<p align="center">
Using this tool, developers can 'paint' objects onto a scene as if they were applying paint to a digital canvas, removing the need to place, rotate and scale objects one at a time.
</p>

<p align="center">
  <img src="https://imgur.com/euzclLv.png">
</p>

## Getting Started
### Terminology
| Term                           | Description |
| -----------                    | ----------- |
| Paint / Painting / Painted     | The action / act of using the tool to place objects onto a scene      |
| Paint Iteration                | A paint iteration is when the tool paints a collection of game objects onto a scene       |
| Painted Game Objects           | Game objects that have been placed onto the scene by the tool       |

### Settings
| Setting            | Description |
| -----------        | ----------- |
| Brush Size         | The size of the brush       |
| Minimum Padding    | The minimum distance between game objects when painted        |
| Max Objects        | The maximum amount of game objects that can be spawned in a single paint iteration       |
| Minimum Rotation   | The minimium Y rotation for painted game objects        |
| Maximum Rotation   | The maximum Y rotation for painted game objects        |
| Minimum Size Scale | The minimum scale that game objects will be multiplied by when painted        |
| Maximum Size Scale | The maximum scale that game objects will be multiplied by when painted       |

### Installing
1) Return to the top of this repostiory and press ![](https://imgur.com/N10rcXd.png) then download the zip file and extract the files.
2) Create or open a Unity project and import the Asset Brush package from the extracted files.

    > Alternatively, open the Unity project labeled `Development` which has the Asset Brush already installed.

### User Guide
1) Go to `Windows` and then `Asset Brush` to open the tool.
2) Adding a game object to the Asset Brush can be done by pressing ![](https://imgur.com/7AnuDH0.png) to select a prefab and then pressing `Add`.
3) To assign a parent for painted game objects, select a game object in the scene hierachy and press `Set selected as parent`.
4) To begin painting, press `Begin painting` and hold `left click` to paint onto a scene.
5) To erase painted game objects, press `Erase painting` and hold `left click` to erase painted objects.
    
    > You can only erase painted game objects that you have painted during your current session.
