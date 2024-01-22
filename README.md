
# VirtualDisk

## Overview

Welcome to VirtualDisk, a C# project developed by Elias Traunbauer. This project is a virtual disk system, where the entire disk is represented as a byte array. It is an excellent tool for understanding file system operations and data management at a fundamental level.

## Features

- **Byte Array Disk Representation**: The entire disk is contained within a byte array, simulating the structure of a physical disk.
- **Name to Block Mapping**: Implements a table that maps file/directory names to specific block numbers within the virtual disk.
- **Linked Block Structure**: Each block can reference a subsequent block, allowing for the storage of data that spans multiple blocks.
- **Directory Support**: Directories are treated similarly to files, with the difference being that the blocks they reference contain references to other files or directories.

## Getting Started

### Prerequisites

- Ensure you have a C# development environment set up.

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/Elias-Traunbauer/VirtualDisk.git
   ```
2. Open the project in your C# development environment and compile the code.

### Usage

- The virtual disk can be used to simulate file system operations, including creating, reading, and writing files and directories. Detailed usage instructions will be provided in the project documentation.
