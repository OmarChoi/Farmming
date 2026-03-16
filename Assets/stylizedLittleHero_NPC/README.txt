================================================================================
  Stylized Little Hero NPC
  Character Customization System
================================================================================

VERSION: 1.1
AUTHOR: HandMadeStudio
SUPPORT: tigerhandstudio@gmail.com

================================================================================
QUICK START GUIDE
================================================================================

WHAT'S NEW IN VERSION 1.1
--------------------------
✓ Character part swapping system
✓ Automatic bone rebinding (fixes mesh renderer issues!)
✓ UI-based customization support
✓ Runtime customization API
✓ 7 customizable parts: Body, Hair, Hat, Eye, Mouth, Eyebrow, Cheek


SETUP IN 3 STEPS
-----------------

1. ADD SCRIPT TO CHARACTER
   - Select your character in Hierarchy
   - Add Component → "CharacterPartSwapper"

2. ASSIGN PREFABS
   - In Inspector, expand "Body Prefabs" array
   - Drag all body FBX files from NPC/Meshes/NPC_body_M or NPC_body_F folder
   - Repeat for Hair, Eye, Mouth, etc.

3. SWAP PARTS
   - Right-click on CharacterPartSwapper component
   - Select "Hair: Next" or "Body: Next" to test
   - Or use the demo scene for UI-based customization


DEMO SCENE
----------
Location: Scenes/CustomizationDemo

This scene demonstrates:
- UI buttons for part switching
- All 7 customizable parts
- Random customization button


SCRIPTING API
-------------

// Get the component
CharacterPartSwapper swapper = GetComponent<CharacterPartSwapper>();

// Change parts
swapper.NextHair();           // Next hair style
swapper.PreviousBody();       // Previous body
swapper.SetEye(2);            // Set to eye index 2
swapper.RandomizeAll();       // Randomize all parts

// Set by name
swapper.SetPartByType("hair", 3);  // Set hair to index 3


TROUBLESHOOTING
---------------

Q: Parts look weird after swapping?
A: Make sure you're using CharacterPartSwapper script, not manually
   dragging meshes. The script handles bone rebinding automatically.

Q: "Rendering stopped" error in console?
A: This is fixed in version 1.1. Make sure you're using the latest
   CharacterPartSwapper.cs script.

Q: How do I add new parts?
A: Just add the FBX to the corresponding array (e.g., Hair Prefabs)
   in the Inspector. No code changes needed!


IMPORTANT NOTES
---------------
- Body parts use SkinnedMeshRenderer with bone rebinding
- Face parts (Hair, Eye, etc.) are instantiated under head_dummy
- All FBX files must have the same bone structure (Bip001 skeleton)
- Use the provided FBX files as templates for custom parts


FILE STRUCTURE
--------------
Assets/
└── stylizedLittleHero_NPC/
    └── NPC/
        ├── Meshes/
        │   ├── NPC_body_M/        (Male body parts)
        │   ├── NPC_body_F/        (Female body parts)
        │   ├── face/              (Eye, Mouth, Eyebrow, Cheek)
        │   ├── hair/              (Hair styles)
        │   ├── weapon/            (Weapons)
        │   └── blacksmith/        (Blacksmith props)
        ├── Prefabs/               (Pre-made character variations)
        ├── Materials/
        ├── Textures/
        └── Animation/
    ├── Scripts/
    │   ├── CharacterPartSwapper.cs      (Main script)
    │   └── CharacterCustomizeUI.cs      (UI helper)
    └── Scenes/


SUPPORT
-------
For questions, bug reports, or feature requests:
Email: tigerhandstudio@gmail.com
Documentation: See Documentation.txt for detailed API reference


LICENSE
-------
Standard Unity Asset Store EULA
https://unity.com/legal/as-terms


================================================================================
Thank you for using Stylized Little Hero NPC!
================================================================================
