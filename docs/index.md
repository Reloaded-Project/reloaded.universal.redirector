<div align="center">
	<h1>Reloaded-II Virtual File System</h1>
	<img src="./images/icon.png" Width=200 /><br/>
	<strong>🎈 Let's screw with binaries 🎈</strong>
    <p>A framework for creating virtual files at runtime.</p>
</div>

## About The Project

The Reloaded Virtual File System (VFS) is an invisible helper that sits
between your games and the files they use. It allows your games to 'see' and 
open files that aren't really 'there'; keeping your game folder unmodified.

```mermaid
flowchart LR

    p[Game] -- Open File --> vfs[Reloaded VFS]
    vfs -- Open Different File --> of[Operating System]
```

The VFS sits in the middle and does some magic 😇.

## Usage Guide

You place files in a folder, and game can 'magically' see them.  
[Please see Usage section for more details.](./usage.md)

## Performance Impact

The Reloaded VFS is optimized for file open operations, with a negligible performance 
difference compared to not using VFS. In a test with opening+closing 21,000 files 
(+70,000 virtualized), the difference was only ~41ms (~3%) or less than 2 microseconds per file.

```md
// All tests done in separate processes for accuracy.
|                           Method |    Mean |    Error |   StdDev | Ratio |
|--------------------------------- |--------:|---------:|---------:|------:|
|           OpenAllHandles_WithVfs | 1.650 s | 0.0102 s | 0.0095 s |  1.03 |
| OpenAllHandles_WithVfs_Optimized | 1.643 s | 0.0145 s | 0.0135 s |  1.03 |
|        OpenAllHandles_WithoutVfs | 1.602 s | 0.0128 s | 0.0120 s |  1.00 |
```

In real-world `"cold-start"` scenarios, this difference is effectively 0%.  
For more benchmarks, please see the [Benchmarks](./benchmarks.md) page.

## How to Contribute (Wiki)

- [Contributing to the Wiki: Online](./guides/contributing-online.md)
- [Contributing to the Wiki: Locally](./guides/contributing-locally.md)

## Credits, Attributions

- Header icon created by <a href="https://www.flaticon.com/free-icons/settings" title="settings icons">Freepik - Flaticon</a>