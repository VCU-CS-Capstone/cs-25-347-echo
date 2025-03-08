*Virtual Fitting Room Sample*
On this scene are the prefab "Mannequin" and clothing samples. When the scene starts, they will be positioned in front of the RGB image from the camera.

You can study how two items of clothing are configured: Jeans and a T-shirt. A "FittingCloth" component is attached to each of them.
In this component, which clothing bones will be associated with the mannequin bones are selected, and some parameters are also set.

Initial check:

1. Start the "Virtual Fitting Room Sample" scene
2. Hide clothes and turn on the skeleton. Make sure that the red points of the skeleton are exactly on top of the joints of the person in the image.
3. Hide skeleton and turn on the mannequin display. Make sure that the model is exactly superimposed on top of the person in the image
4. Hide mannequin and turn on the clothes display.

Development (for the test, you can use the model Cloth->Models->T-Shirt.fbx):

- Select the "Mannequin" prefab in "hierarchy". Each blue sphere on the skeleton corresponds to a joint that can be tracked using Nuitrack
(you can also see the joints scheme on Mannequin->Nuitrack Avatar component)
- Enable "Mesh" object in "Mannequin" prefab
- Learn the prefabs of clothes on scene. Note that these are skeletal meshes.
- The clothing model should have bones in the same places as on the mannequin. 
- To create a model from scratch, you can use "mannequin.blend", there is the most correct skeleton
- third-party models may lack a collar joint in the center between the shoulders, then you need to create it manually. 
Create an empty object and insert it into the hierarchy of the model. You can see how it is made for a T-shirt on scene ("FakeCollarBone" object)
- Add your clothing model to the scene. Move the clothes to the mannequin. The clothes must be correctly superimposed on the mannequin.
- If the model has incorrect proportions, you need to edit it in a 3D editor so that it looks correctly on the mannequin.
- Then add the "FittingCloth" component to the clothing model.
Turn on "Update Joints Transform In Editor" so that clothes are automatically attracted to the mannequin right in the editor
- Find the "Skinned Mesh Renderer" component (usually located on an object inside the model) and select Quality = 4 bones
- Expand the settings of the necessary joints. Move the corresponding "Mannequin" bones to the "Target Mannequin Bone" fields.
- Move the corresponding clothing bones to the "Target Cloth Bone" fields.
If after adding some bones, the model seems to be curved, try adding a shift to the "Rotation Offset"
rotation - Start the scene or rotate the mannequin's bones in the editor to check the clothes

Components:

ClothBone - allows you to bind any object to any bone of the "Mannequin". It is convenient for individual elements that do not have a large hierarchy of bones (watches, hats, etc.)
FittingCloth - component allows you to link several clothing bones to the corresponding "Mannequin" bones.