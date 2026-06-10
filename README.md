# Unity-HDRP-Texture-Channel-Packers
In case you have a seperate opacity map and a base (diffuse/Albedo) map. You'd need to combine them into 1 to use the transparancy in the hdrp lit shader. This does that for you


# Showcase
- Go from this
<img src=https://github.com/DriesDeRidder/Unity-HDRP-Opacity-Albedo-packer/assets/32333783/4237b56e-ebc3-41e5-8df6-6ce06418fc6c  width="387" >

- To this
<img src=https://github.com/DriesDeRidder/Unity-HDRP-Opacity-Albedo-packer/assets/32333783/746684d3-b2e1-41e3-b7f7-98430cc4694c  width="387" >



# How to use
- place the script in an editor folder 
- Open me by going to Tools/Opacity Albedo Packer
- place the textures in the respective slots
<img src=https://github.com/DriesDeRidder/Unity-HDRP-Opacity-Albedo-packer/assets/32333783/ddfd58b9-0c4d-482e-96c0-ca252a3fadb7  width="387">



- Make sure the opacity map has it's transparency properly set up (either set 'Alpha Source' to 'from grayscale' or to  'Input Texture Alpha')
- Either way your preview should show the transparent squares in the preview
<img src=https://github.com/DriesDeRidder/Unity-HDRP-Opacity-Albedo-packer/assets/32333783/2e1f7367-c91a-4a71-9b2d-2bb4d0d94998  width="387">

- Hit Combine Textures.

- It'll create a new texture with 'combined' at the end.
<img src=https://github.com/DriesDeRidder/Unity-HDRP-Opacity-Albedo-packer/assets/32333783/f02a02dd-29a6-4b4d-949e-afc6e22b2728  width="387">

- Place this texture in the 'base map' slot.
- Make sure alpha clipping is enabled in the material shader.
