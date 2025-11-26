# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is an **Advanced Computer Graphics framework** for real-time 3D rendering using OpenGL 3.3+, designed for educational purposes at UPF. The framework supports:
- Standard mesh rendering with Phong lighting
- Volume rendering with raymarching (homogeneous and heterogeneous volumes)
- OpenVDB file loading for volumetric data
- Interactive camera controls and ImGui-based scene editor

## Build System

### Windows
```powershell
mkdir build
cd build
cmake ..
```
Open the generated `.sln` file in Visual Studio to build and debug.

**Working directory:** The VS debugger working directory is automatically set to the project root by CMake.

**Path issues on Mac:** XCode may fail to load assets. Change asset paths from `"res/meshes/sphere.obj"` to `"../../res/meshes/sphere.obj"`.

### macOS
```bash
mkdir build && cd build
cmake -G "Xcode" ..
```
Open the generated `.xcodeproj` in Xcode.

### Linux
```bash
mkdir build && cd build
cmake ..
make -j8
```

### Dependencies
All dependencies are managed via git submodules in `libraries/`:
- **GLFW** (windowing)
- **GLEW** (OpenGL extensions)
- **GLM** (math library)
- **ImGui** (GUI)
- **ImGuizmo** (3D gizmos, fetched via CMake)
- **easyVDB** (OpenVDB reader)

## Common Commands

### Reload Shaders
Press `R` at runtime to hot-reload all shaders without restarting.

### Single Test Development
The framework has no formal test suite. Test changes by modifying `Application::init()` in `src/application.cpp` to create test scenes.

## Code Architecture

### Application Lifecycle
- **Entry point:** `src/main.cpp` sets up GLFW window, GLEW, ImGui, then calls `Application::init()` 
- **Main loop:** `Application::update()` → `Application::render()` → ImGui rendering
- **Singleton pattern:** `Application::instance` is a global singleton accessed throughout the codebase

### Scene Graph
- **SceneNode** (`framework/scenenode.h`): Base class for all renderable objects
  - Contains `model` matrix, `mesh`, `material`, and `visible` flag
  - Subclass types: `NODE_BASE`, `NODE_VOLUME`, `NODE_LIGHT`
- **Light** (`framework/light.h`): Extends `SceneNode` with `intensity` property
- **Application** holds `std::vector<SceneNode*> node_list` for all scene objects

### Rendering Pipeline
1. **Material System** (`graphics/material.h`):
   - `Material::setUniforms()` uploads uniforms to shaders
   - `Material::render()` binds shader/textures and draws mesh
   - Types: `FlatMaterial`, `StandardMaterial`, `VolumeMaterial`, `WireframeMaterial`

2. **Volume Rendering** (`graphics/material.h`, `res/shaders/volume.fs`):
   - Uses raymarching through AABB in local space
   - Homogeneous volumes: analytical transmittance calculation
   - Heterogeneous volumes: step-by-step integration with procedural noise or 3D textures
   - OpenVDB support via `VolumeMaterial::loadVDB()` → converts to 3D texture via `estimate3DTexture()`

3. **Shader Management** (`graphics/shader.h`):
   - `Shader::Get()` uses a global cache (`s_Shaders`) to avoid recompilation
   - `Shader::ReloadAll()` recompiles all loaded shaders (triggered by `R` key)
   - Supports macros for shader variants

### Camera System
- **Camera** (`framework/camera.h`): Handles view/projection matrices
  - `orbit(yaw, pitch)` for mouse-drag rotation
  - Scroll to zoom (adjusts FOV)
  - `Camera::current` is a static pointer to the active camera

### Resource Management
- **Mesh** (`graphics/mesh.h`): Loads OBJ files, stores VAO/VBO data
- **Texture** (`graphics/texture.h`): Loads TGA/PNG, supports 2D/3D/Cubemap textures
  - 3D textures used for volumetric data
  - `Texture::Get()` uses global cache `sTexturesLoaded`

## File Structure

```
src/
├── main.cpp                    # Entry point, GLFW/ImGui setup
├── application.{h,cpp}         # Main application logic, scene management
├── framework/
│   ├── camera.{h,cpp}          # Camera controls and matrices
│   ├── light.{h,cpp}           # Light node
│   ├── scenenode.{h,cpp}       # Base scene graph node
│   ├── utils.{h,cpp}           # Grid rendering, file I/O helpers
│   └── includes.h              # Common OpenGL/GLEW/GLFW includes
└── graphics/
    ├── mesh.{h,cpp}            # Mesh loading and rendering
    ├── shader.{h,cpp}          # Shader compilation and management
    ├── texture.{h,cpp}         # Texture loading (2D/3D)
    └── material.{h,cpp}        # Material types (Flat, Standard, Volume)

res/
├── meshes/                     # OBJ models (cube.obj, sphere.obj, etc.)
├── shaders/                    # GLSL vertex/fragment shaders
│   ├── basic.{vs,fs}           # Simple shaders
│   ├── flat.fs                 # Flat color
│   ├── normal.fs               # Normal visualization
│   ├── volume.fs               # Volume raymarching (absorption-only)
│   ├── volume_emission.fs      # Volume with emission
│   └── volume_texture.fs       # Volume with 3D texture sampling
└── volumes/                    # OpenVDB files (e.g., bunny_cloud.vdb)
```

## Development Patterns

### Adding a New Scene Node
```cpp
// In Application::init():
SceneNode* myNode = new SceneNode("My Node");
myNode->mesh = Mesh::Get("res/meshes/sphere.obj");
myNode->material = new StandardMaterial(glm::vec4(1.0f, 0.0f, 0.0f, 1.0f));
myNode->model = glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 1.0f, 0.0f));
this->node_list.push_back(myNode);
```

### Creating a Volume Node
```cpp
SceneNode* volume = new SceneNode("Volume");
volume->mesh = Mesh::Get("res/meshes/cube.obj");
VolumeMaterial* mat = new VolumeMaterial();
mat->volume_type = 0;  // 0=homogeneous, 1=heterogeneous
mat->absorption_coefficient = 2.0f;
mat->step_length = 0.005f;
// Optional: Load VDB file
// mat->loadVDB("res/volumes/bunny_cloud.vdb");
volume->material = mat;
this->node_list.push_back(volume);
```

### Shader Development
- Shaders are in `res/shaders/` with `.vs` (vertex) and `.fs` (fragment) extensions
- Use `Shader::Get("res/shaders/myshader.vs", "res/shaders/myshader.fs")` to load
- Access uniforms via `shader->setUniform("u_uniform_name", value)`
- Always check shader compilation with `shader->compiled` and `shader->getInfoLog()`

## Important Notes

- **C++20 Standard:** The project uses C++20 features (set in CMakeLists.txt)
- **OpenGL 3.3 Core:** Minimum version, uses modern VAO/VBO patterns
- **ImGui Integration:** All scene parameters exposed via `renderInMenu()` methods
- **Asset Paths:** Relative to project root (working directory)
- **Error Checking:** Use `checkGLErrors()` from `utils.h` after OpenGL calls during debugging
- **Memory Management:** Manual `new`/`delete` used throughout (no smart pointers)
- **Submodule Initialization:** Clone with `--recurse-submodules` flag or run `git submodule update --init --recursive`

## Volume Rendering Details

The volume rendering system (`VolumeMaterial`) performs raymarching in the fragment shader:

1. **Ray Setup:** Transform camera ray from world space to volume's local space using `inverse(u_model)`
2. **AABB Intersection:** Find entry/exit points with the bounding box
3. **Marching:** Sample density/absorption along the ray at intervals of `step_length`
4. **Accumulation:** Compute optical thickness and transmittance using Beer-Lambert law
5. **3D Textures:** For VDB files, density is sampled from a 3D texture generated by `estimate3DTexture()`

Key uniforms in volume shaders:
- `u_absorption_coefficient`: Controls opacity
- `u_step_length`: Ray marching step size (smaller = higher quality, slower)
- `u_volume_type`: 0 for homogeneous, 1 for heterogeneous (with noise or texture)
- `noise_scale`: Controls frequency of procedural noise in heterogeneous volumes
