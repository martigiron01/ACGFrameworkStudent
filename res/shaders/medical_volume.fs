#version 330 core

in vec3 v_world_position;
in vec3 v_normal;
in vec4 v_color;
in vec2 v_uv;

out vec4 FragColor;

uniform vec3 u_camera_position;
uniform vec4 u_color;
uniform mat4 u_model;
uniform vec4 u_background_color;
uniform int u_num_steps;
uniform float u_step_length;
uniform vec3 u_box_min;
uniform vec3 u_box_max;

uniform sampler3D u_texture;

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

vec3 transferFunction(float density) {
    //Bones
    if (density < -500) {
        return vec3(1.0); // White
    }
    //Lungs
    else if (density >= -500 && density < 0) {
        return vec3(1.0, 0.0, 1.0); // Purple
    }
    //Heart
    else if (density >= 0 && density < 300) {
        return vec3(1.0, 0.0, 0.0); // Red
    }
    //Air
    else {
        return vec3(0.0); // Black
    }
}

void main() {
    // Initialize ray in world space
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    // Transform ray into local space
    mat4 invModel = inverse(u_model);
    vec3 rayOriginLoc = (invModel * vec4(rayOrigin, 1.0)).xyz;
    vec3 rayDirLoc = normalize((invModel * vec4(rayDir, 0.0)).xyz);

    // Compute intersection with box in local space
    vec2 intersection = intersectAABB(rayOriginLoc, rayDirLoc, u_box_min, u_box_max);
    float tEntry = intersection.x;
    float tExit = intersection.y;

    // If no intersection, discard fragment
    if (tExit < 0.0 || tEntry > tExit)
        discard;

    // Sampling parameters
    float dt = u_step_length;

    // Optical thickness
    float thickness = 0.0;
    vec3 L = vec3(0.0); 

    for (float t = tEntry + 0.5 * dt; t < tExit; t += dt)
    {
        vec3 point = rayOriginLoc + t * rayDirLoc; 
        
        // Map from bounding box local space to texture space [0, 1]
        vec3 pointTex = (point + 1.0) / 2.0;
        
        // Sample the 3D texture (GL_R8 auto-normalizes to [0,1])
        float density = texture(u_texture, pointTex).r;
        
        vec3 color = transferFunction(density);

        thickness += density * dt;
    }

    float transmittance = exp(- thickness);

    // Final color
    vec3 background = u_background_color.rgb;

    vec3 finalColor = background * transmittance;

    // Discard if almost fully transparent
    if (transmittance > 0.99) {
        discard;
    }
    
    FragColor = vec4(finalColor, u_color.a);

}