# Save and Loading System
This project was made in Unity using C# for a masters thesis at HTW Berlin, Germany.

## About the Project
The Save System is a comprehensive and versatile tool designed specifically for Unity developers who need a reliable solution for saving game data. At its core, the system uses JSON for data serialization, which makes the saved data inherently platform-agnostic, ensuring that it can be easily transferred and used across various environments without compatibility issues.

One of the standout features of this Save System is its ability to handle complex object references. In many games, objects are interconnected, with dependencies and relationships that need to be preserved when saving and loading data. This system excels in managing these relationships, even when they involve nested or circular references, which are often challenging to handle. By accurately restoring these references during the loading process, the system ensures that the game's state is fully reconstructed, including the dynamic reloading of any prefabs that were part of the saved state.

Additionally, the Save System is designed to support as many data types as possible, making it highly adaptable to different game structures and requirements. This flexibility allows developers to save a wide variety of data, from simple variables to complex object graphs, ensuring that all necessary information is preserved.

Moreover, the Save System offers various approaches to saving data, allowing developers to choose the method that best suits their specific needs and the complexity of their game. Whether it's through declarative Attribute Saving, using the ISavable interface, or employing Type Converters, the system provides flexible options to ensure that data is efficiently and accurately preserved across different game states. This means that while developers can quickly implement basic saving and loading functionalities with minimal effort, they also have the option to customize the process.

UPM Package: https://github.com/AyuCalices/UnitySaveLoadSystem.git?path=Assets/SaveMate
