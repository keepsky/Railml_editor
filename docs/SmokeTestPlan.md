# RailML Editor Smoke Test Plan

This document outlines a manual testing procedure (Smoke Test) to verify that the core functionalities of the RailML Editor are working correctly after a new build or major change.

## 1. Project Management
1. **Launch**: Start the application. The application should load without crashing.
2. **New Project**: 
   - Click `File > New Project`. 
   - Verify a new tab (e.g., `notitle-1.railml`) opens and becomes the active tab.
3. **Open Project**:
   - Click `File > Open`. 
   - Select a valid RailML file.
   - Verify that all elements load accurately and display on the canvas inside a new tab.

## 2. Element Manipulation
1. **Add Track**:
   - Drag a Straight Track from the Toolbox to the canvas.
   - Select the track. Verify the Property Grid populates with `Id`, `Name`, X/Y coordinates, and `Length`.
   - Update `Length` to 500. Verify the track visually extends.
2. **Add Curve**:
   - Drag a Curved Track onto the canvas.
   - Adjust the `Radius` and `Angle` properties.
   - Verify the curved shape updates dynamically.
3. **Add Signals & Borders**:
   - Snap a Signal to an existing track end.
   - Verify it establishes a connection (Parent Track ID is reflected).

## 3. Topology & Switches
1. **Create Switch**:
   - Place two tracks near each other such that their ends meet.
   - Verify that the Topology Builder automatically recognizes the joint and creates a Switch node.
   - A selection dialog should ask which track is the "Principle" track. Select one.
   - Verify the switch is formed correctly with Entering, Principle, and Diverging paths mapped.

## 4. Multi-Tab Environment
1. **Switch Context**:
   - Open two different projects.
   - Add a track in `Tab 1`. Switch to `Tab 2`.
   - Verify the track from `Tab 1` does NOT appear in `Tab 2` and the `ActiveDocument` updates appropriately.

## 5. Save and Export
1. **Save Project**:
   - Make a change (e.g., add a track).
   - Go to `File > Save` or `Save As`.
   - Save the `.railml` file.
   - Inspect the XML file in a text editor to ensure new elements and nodes are properly serialized in RailML format.

*If all steps pass without errors, the application is deemed stable for core usage.*
