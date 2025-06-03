# FileIndexerProject

## Overview
This project is a distributed system written in .NET 6 consisting of three console applications:
- **AgentA**: Scans a folder and sends indexed word data via named pipe.
- **AgentB**: Same as AgentA but independent.
- **Master**: Receives data from both agents, aggregates, and displays the result.

## How to Run
1. Build all three projects using .NET 6 SDK.
2. Run `Master`, passing the pipe names.
3. Run `AgentA` and `AgentB`, passing folder paths.
