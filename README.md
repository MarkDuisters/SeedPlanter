# Surface Object Populator

## Overview

This is an editor script designed for Unity that allows you to populate a surface with objects easily. Unlike traditional terrain tools, this script does **not** create terrain instances but instead spawns **GameObjects** in your scene.

## Features

- **Easy Setup**: Simply create a `SeedObjectData` file via `Create -> ScriptableObjects`.
- **Prefab-Based**: Assign any prefab you want and tweak its settings.
- **Flexible Usage**: While intended for trees and foliage, it works with **any** prefab.
- **Customizable Spawning**: The populator/planter takes `SeedObjectData` settings into account when distributing objects.

## Installation

1. Download or clone this repository.
2. Import the script into your Unity project.
3. Create a `SeedObjectData` file (`Create -> ScriptableObjects`).
4. Assign your desired prefab and adjust settings.
5. Use the populator to generate objects in your scene.

## Usage

1. Open Unity and navigate to the ScriptableObjects menu.
2. Create a new `SeedObjectData` file.
3. Set a prefab and modify its settings.
4. Use the populator tool to spawn objects based on the configured settings.

## Notes

- This tool **does not modify terrain data**, it only spawns GameObjects.
- Works best for **natural elements** like trees, grass, and rocks but supports any prefab.
- Adjust settings carefully to optimize performance, especially for large object counts.

## License

This project is open-source. Feel free to modify and use it in your projects.

---

Have fun! 🌲🌿🏡

![alt text](https://github.com/MarkDuisters/SeedPlanter/blob/main/images/create%20seed.gif)
![alt text](https://github.com/MarkDuisters/SeedPlanter/blob/main/images/plant%20trees.gif)
<img src="https://github.com/MarkDuisters/SeedPlanter/blob/main/images/place%20planter.gif" width="512" height="1090">
