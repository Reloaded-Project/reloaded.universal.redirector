# About the Reloaded Virtual FileSystem

!!! info

    Below are some characteristics of the virtual filesystem.

Compared to Windows symlinks/hardlinks:  

- Links are only visible to the current application.  
- Write access to game folder is not needed. Can even link new content into read-only folders.  
- Administrator rights are not needed.  
- Can overlay multiple directories ontop of the destination.  

And with the following benefits:  

- Easy to use API for programmers.  
- Practically zero overhead.  
- Can add/remove and remap files on the fly (without making changes on disk).  

👍 Also supports Wine on Linux.

## Limitations

!!! info

    Write functionality in VFS scenarios can be ambiguous.

For example, if you save a new file, where should it go?

- Original Folder?
- Some Mod's Folder?
  - What if a folder is shared amongst multiple mods?

To avoid these complications, Reloaded VFS only implements *well defined*
functionality, effectively becoming a ***read-only*** VFS.

!!! question

    But what does this mean?

Well, quite simply. Any write operations, such as creating a file are unaffected.

If a game wants to write a new file (such as a savefile), no action will be taken
and the file will be written to the game folder. If a native DLL plugin wants to write
a config file, it will write it to the game folder, as normal. That simple.

### Warning

!!! caution

    Proceed with care if any of the following applies:  

- If your game's modding tools operate on a modified game directory (e.g. Skyrim xEdit), 
  using VFS is not recommended as new files might be written to the game folder.

- Do not use VFS to redirect files deleted and then recreated by games; 
  ***you will lose the files from inside your mod***.

### Error Cases

!!! failure Unsuitable

    Using this VFS is not appropriate for your game if any of the following is true.

- This VFS does not handle child processes. 
  Do not use VFS for games that can run external tools with virtualized files.

### Information

!!! info Additional Limitations

    The following limitations should not cause concern.

- Reloaded VFS does not support Reparse Point Tags.  
    - However, this shouldn't cause issues with mods stored on cloud/OneDrive/etc.  
- Reloaded VFS does not return 8.3 DOS file names for virtualized files.  

## File Write Behaviours

!!! info

    Reloaded VFS is a read-only VFS, so what happens when you try editing files?

| Description                        | Action Performed                                                                                       |
|------------------------------------|--------------------------------------------------------------------------------------------------------|
| File Deletion                      | Delete the mod file instead of the original file                                                       |
| New File Creation                  | Create new files in the original game folder                                                           |
| File Editing                       | Edits the redirected file                                                                              |
| File Delete & Recreate (New)       | Delete the overwritten file and place the new file in game folder                                      |
| Renaming Folders to Other Location | Either move the original folder or files in original and overlaid folders (depends on how API is used) |