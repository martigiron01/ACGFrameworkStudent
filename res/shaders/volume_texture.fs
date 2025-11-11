#version 330 core

in vec3 v_world_position;
in vec3 v_normal;
in vec4 v_color;
in vec2 v_uv;

out vec4 FragColor;

uniform int u_volume_type = 0; // 0: homogeneous, 1: heterogeneous
uniform vec3 u_camera_position;
uniform vec4 u_color;
uniform float u_absorption_coefficient;
uniform mat4 u_model;
uniform vec4 u_background_color;
uniform int u_num_steps;
uniform float u_step_length;
uniform float noise_scale;
uniform float noise_amplitude;
uniform vec3 u_box_min;
uniform vec3 u_box_max;




vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax)
{
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

// Texture value for each sample position along the ray
void main()

