# Frog Evolution

Games and Artificial Intelligence Techniques
Assignment 2

Students:
Michael Dann (s3365362)
Jayden Ivanovic (s3331571)

# Report
Please refer to the attached file "Report.pdf" for our write up of the game.

# Running the game
To run the new game that we created for the second assignment, please launch
Unity from the scene "MenuA2".

When clicking play you will be presented with a loading screen. The game takes
a while to initialize as the GA is generating the positions for a number of
trees and flowers.

Flies can be selected by left clicking with the mouse and dragging the box over
the flies you wish to move. Additionally, if you hit the numbers 1,2,3,4 and 5,
you will get that many flies from the ones currently selected which are the
closest to your current mouse position. For example, if you have 5 flies
selected and press 1, you will select the closest fly to your mouse position
and the others will be unselected.

Flies will get points when hovering around the trees with flowering/apples
(white/red trees). Return your flies to the tree in the center in order to
return these points. For every 100 points you return a new fly will be spawned.


===============
 Training mode
===============

To see our setup for training the enemy frog, please open the "Training" scene.

Since training the frogs takes quite some time, we have included some
pre-trained populations for demonstration purposes. To toggle between the
various sample populations, click on the "GAController" GameObject and set the
"Load Epoch" parameter in the "GAFrog Controller" script to a value in the set
{0, 100, 200, ... , 2000} before launching the scene. (The program saves the
population after every epoch but we only included multiples of 100 to keep the
project's file size down.)

To train from scratch, clear out the "Load Path" parameter on the "GAController" GameObject.

# Known issue
Since the main focus of this assignment was machine learning, we did not spend
as much time polishing the actual game as we did for assignment 1. As such,
there are still a few issues / bugs we haven't fixed:

- The tree placement genetic algorithm occasionally spawns trees in the middle
  of lakes.
- The enemy frog occasionally gets trapped in a corner by both snakes.
- The frog never dies

# Main scripts
There are a LOT of script components in our project, and many of them are not
particularly relevant to assignment 2. The most important scripts for this
assignment are as follows -

Main game:
- Assets/Scripts/DragSelectFlies.cs
- Assets/Scripts/FlyPlayerInfo.cs

Tree placement:
- Assets/Scripts/GeneticAlgs/GAController.cs
- Assets/Scripts/GenerateFoodTrees.cs

Frog training:
- Assets/Scripts/GeneticAlgs/GAFrogController.cs
- Assets/Scripts/ManagePen.cs
- Assets/Scripts/NeuralNet.cs
- Assets/Scripts/Steering/NeuralNetSteering.cs

Based on Justin's feedback from the first assignment, we avoided using
GetComponent in code segments that get called every frame. However, we did not
go back and fix all the GetComponents in the first assignment's code because
there were sooo many and we weren't experiencing any serious frame rate issues
(yet). This was a definitely a lesson learned for the next major Unity project
that either of us write!
