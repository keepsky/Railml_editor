# RailML Editor

A WPF-based desktop application for editing and visualizing railway infrastructure files in RailML format.

## Overview
RailML Editor provides a comprehensive environment for designing and managing railway infrastructure. It allows users to place elements like tracks, signals, and switches on a canvas, configure their properties, and define complex routes. The application is designed to be compliant with the RailML 2.5 standard for data persistence.

## Key Features
- **Infrastructure Elements**: Add and configure Tracks (Straight/Curved), Signals, and Points (Switches).
- **Route Management**: Advanced interface for defining Routes, including switch alignment, overlap settings, and release sections.
- **Intuitive GUI**:
  - Drag-and-drop toolboxes for easy element placement.
  - Explorer-style tree view for hierarchical navigation.
  - Dynamic property grid for detailed configuration.
  - Smooth pan and zoom canvas.
- **RailML Standard**: Save and load infrastructure data in standard RailML 2.5 XML format.
- **Modern UI**: Custom-styled TreeView and themed toolbar icons for a premium look and feel.

## Architecture

After recent structural refactoring, the project adheres closely to MVVM and the Single Responsibility Principle.

### System Architecture Diagram

```mermaid
flowchart TD
    subgraph UI ["Views (UI Layer)"]
        MW[MainWindow.xaml]
        EV[ExplorerView]
        PV[PropertiesView]
    end

    subgraph VM ["ViewModels (Presentation Logic)"]
        MVM[MainViewModel]
        DVM[DocumentViewModel]
        BEVM[BaseElementViewModel]
    end

    subgraph Svc ["Logic & Services (Business Rules)"]
        CIC[CanvasInteractionController]
        EFS[ElementFactoryService]
        RS[RailmlService]
        TM[TopologyManager]
    end

    subgraph Data ["Models (Data Contracts)"]
        RM[RailModel XML]
        AS[AppSettings]
    end

    UI -->|"DataBinding & Commands"| VM
    UI -->|"Raw Mouse Events"| CIC
    
    CIC -->|"MoveBy / Select"| BEVM
    MVM -->|"Create Element"| EFS
    EFS -->|"Instantiate"| BEVM
    VM -->|"Save/Load"| RS
    RS -->|"Serialize/Deserialize"| RM
    TM -->|"Update Connections"| BEVM
```

### Class Diagram: ViewModels

```mermaid
classDiagram
    class BaseElementViewModel {
        +String Id
        +String Name
        +MoveBy(deltaX, deltaY)
    }
    class TrackViewModel {
        +TrackNodeViewModel BeginNode
        +TrackNodeViewModel EndNode
        +MoveBy(deltaX, deltaY)
    }
    class CurvedTrackViewModel {
        +Double MX
        +Double MY
        +MoveBy(deltaX, deltaY)
    }
    class SwitchViewModel {
        +List DivergingConnections
    }
    class DocumentViewModel {
        +ObservableCollection Elements
        +UndoRedoManager History
    }
    class MainViewModel {
        +ObservableCollection Documents
        +DocumentViewModel ActiveDocument
    }

    BaseElementViewModel <|-- TrackViewModel
    TrackViewModel <|-- CurvedTrackViewModel
    BaseElementViewModel <|-- SwitchViewModel
    DocumentViewModel --> "*" BaseElementViewModel : Contains
    MainViewModel --> "*" DocumentViewModel : Manages
```

### Class Diagram: Mappers

RailML mapping logic has been extracted into highly cohesive interface-bound classes.

```mermaid
classDiagram
    class RailmlService {
        +Save(path, ViewModel, Doc)
        +Load(path, ViewModel, Doc)
    }
    class RailmlMapper {
        +ToRailml(ViewModel, Doc)
        +LoadIntoViewModel(Railml, ViewModel, Doc)
    }
    class IRailmlElementMapper~TViewModel, TRailmlElement~ {
        <<interface>>
        +MapToRailml(TViewModel, TRailmlElement, MappingContext)
        +MapToViewModel(TRailmlElement, TViewModel, MappingContext)
    }
    class TrackMapper
    class SwitchMapper
    class SignalMapper

    RailmlService --> RailmlMapper : Orchestrates
    RailmlMapper --> IRailmlElementMapper : Delegates to
    IRailmlElementMapper <|.. TrackMapper
    IRailmlElementMapper <|.. SwitchMapper
    IRailmlElementMapper <|.. SignalMapper
```

### Sequence Diagram: Saving a Document

```mermaid
sequenceDiagram
    participant UI as MainWindow
    participant RS as RailmlService
    participant RM as RailmlMapper
    participant TM as TrackMapper
    participant TB as RailmlTopologyBuilder
    
    UI->>RS: Save(path, MainVM, DocVM)
    RS->>RM: ToRailml(MainVM, DocVM)
    RM->>RM: Create MappingContext
    RM->>TM: MapToRailml(TrackVM, TrackObj)
    TM-->>RM: return TrackObj
    RM-->>RS: return Railml
    RS->>TB: BuildTopology(Railml, DocVM)
    TB-->>RS: Update Connections & Switches
    RS->>RS: Serialize Railml to XML
    RS-->>UI: Persistence Complete
```

### Sequence Diagram: Canvas Interaction (Drag & Drop)

```mermaid
sequenceDiagram
    participant User
    participant View as MainWindow (Canvas)
    participant CIC as CanvasInteractionController
    participant VM as BaseElementViewModel
    
    User->>View: MouseDown on Element
    View->>CIC: HandleMouseDown(MouseEventArgs)
    CIC->>CIC: Detect hit & Element Type
    CIC->>VM: IsSelected = true
    
    User->>View: MouseMove
    View->>CIC: HandleMouseMove(MouseEventArgs)
    CIC->>CIC: Calculate deltaX, deltaY
    CIC->>VM: MoveBy(deltaX, deltaY)
    
    User->>View: MouseUp
    View->>CIC: HandleMouseUp(MouseEventArgs)
    CIC->>CIC: Snap to Grid (if enabled)
    CIC-->>User: Visual Update via DataBinding
```

## Getting Started

### Prerequisites
- .NET 6.0 or higher
- Visual Studio 2022 (recommended)
- *Note: For Windows/WSL specific build instructions, see [Build Policies](docs/BuildPolicies.md)*
- *Note: Before releasing, follow the [Smoke Test Plan](docs/SmokeTestPlan.md)*

### Installation
1. Clone the repository.
2. Open `RailmlEditor.sln` in Visual Studio.
3. Build and run the project.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
