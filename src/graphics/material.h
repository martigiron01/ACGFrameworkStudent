#pragma once

#include <glm/vec3.hpp>
#include <glm/vec4.hpp>
#include <glm/matrix.hpp>

#include "../framework/camera.h"
#include "mesh.h"
#include "texture.h"
#include "shader.h"

#include "../libraries/easyVDB/src/openvdbReader.h"
#include "../libraries/easyVDB/src/grid.h"
#include "../libraries/easyVDB/src/bbox.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	glm::vec4 color;

	virtual void setUniforms(Camera* camera, glm::mat4 model) = 0;
	virtual void render(Mesh* mesh, glm::mat4 model, Camera* camera) = 0;
	virtual void renderInMenu() = 0;
};

class FlatMaterial : public Material {
public:

	FlatMaterial(glm::vec4 color = glm::vec4(1.f));
	~FlatMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

class WireframeMaterial : public FlatMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
};

class StandardMaterial : public Material {
public:

	bool first_pass = false;

	bool show_normals = false;
	Shader* base_shader = NULL;
	Shader* normal_shader = NULL;

	StandardMaterial(glm::vec4 color = glm::vec4(1.f));
	~StandardMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

class VolumeMaterial : public FlatMaterial {
public:

    float absorption_coefficient;
	float scattering_coefficient;
	int shader_type = 0; // 0: absorption only, 1: absorption + emission
	int volume_type = 0; // 0: homogeneous, 1: heterogeneous
	float step_length = 0.1f;
	float noise_scale = 3.0f;
	float g_value = 0.0f; // Scattering anisotropy

    VolumeMaterial(glm::vec4 color = glm::vec4(0.f), float absorption_coefficient = 0.5f, float scattering_coefficient = 0.5f, int volume_type = 0);
    ~VolumeMaterial();

	void render(Mesh* mesh, glm::mat4 model, Camera* camera) override;
    void setUniforms(Mesh* mesh, Camera* camera, glm::mat4 model);
    void renderInMenu() override;

	void loadVDB(std::string file_path);
	void estimate3DTexture(easyVDB::OpenVDBReader* vdbReader);
};

class MedicalMaterial : public FlatMaterial {
public:
	float step_length = 0.04f;
	glm::vec3 plane = glm::vec3(0.f);
	float cutoff = 0.0f;
	MedicalMaterial(glm::vec4 color = glm::vec4(1.f));
	~MedicalMaterial();

	void setUniforms(Mesh* mesh, Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();

	//void loadDCMs(std::string file_path);
};