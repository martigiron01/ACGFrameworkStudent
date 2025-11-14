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
uniform vec3 u_box_min;
uniform vec3 u_box_max;

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;

uniform sampler3D u_texture;

//vec3 texturePoint = texture(u_texture, vec3(0.5, 0.5, 0.5)).xyz;

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

    //	Simplex 3D Noise 
//	by Ian McEwan, Stefan Gustavson (https://github.com/stegu/webgl-noise)
//
vec4 permute(vec4 x){return mod(((x*34.0)+1.0)*x, 289.0);}
vec4 taylorInvSqrt(vec4 r){return 1.79284291400159 - 0.85373472095314 * r;}

float snoise(vec3 v){ 
  const vec2  C = vec2(1.0/6.0, 1.0/3.0) ;
  const vec4  D = vec4(0.0, 0.5, 1.0, 2.0);

// First corner
  vec3 i  = floor(v + dot(v, C.yyy) );
  vec3 x0 =   v - i + dot(i, C.xxx) ;

// Other corners
  vec3 g = step(x0.yzx, x0.xyz);
  vec3 l = 1.0 - g;
  vec3 i1 = min( g.xyz, l.zxy );
  vec3 i2 = max( g.xyz, l.zxy );

  //  x0 = x0 - 0. + 0.0 * C 
  vec3 x1 = x0 - i1 + 1.0 * C.xxx;
  vec3 x2 = x0 - i2 + 2.0 * C.xxx;
  vec3 x3 = x0 - 1. + 3.0 * C.xxx;

// Permutations
  i = mod(i, 289.0 ); 
  vec4 p = permute( permute( permute( 
             i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
           + i.y + vec4(0.0, i1.y, i2.y, 1.0 )) 
           + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

// Gradients
// ( N*N points uniformly over a square, mapped onto an octahedron.)
  float n_ = 1.0/7.0; // N=7
  vec3  ns = n_ * D.wyz - D.xzx;

  vec4 j = p - 49.0 * floor(p * ns.z *ns.z);  //  mod(p,N*N)

  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

  vec4 x = x_ *ns.x + ns.yyyy;
  vec4 y = y_ *ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);

  vec4 b0 = vec4( x.xy, y.xy );
  vec4 b1 = vec4( x.zw, y.zw );

  vec4 s0 = floor(b0)*2.0 + 1.0;
  vec4 s1 = floor(b1)*2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));

  vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
  vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

  vec3 p0 = vec3(a0.xy,h.x);
  vec3 p1 = vec3(a0.zw,h.y);
  vec3 p2 = vec3(a1.xy,h.z);
  vec3 p3 = vec3(a1.zw,h.w);

//Normalise gradients
  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;

// Mix final noise value
  vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
  m = m * m;
  return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1), 
                                dot(p2,x2), dot(p3,x3) ) );
}


float getAbsorption(vec3 point)
{
    float noise = snoise(point * noise_scale);
    noise *= u_absorption_coefficient;
    return max(0.0, noise);
}

void main()
{
    if (u_volume_type == 0) {
        
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

        // Optical thickness
        float thickness = max(0.0, tExit - tEntry);
        // Transmittance
        float transmittance = exp(- thickness * u_absorption_coefficient);

        // Final color
        vec3 background = u_background_color.rgb;
        vec3 emission = u_color.rgb;
        vec3 finalColor = background * transmittance + emission * (1.0 - transmittance);

        FragColor = vec4(finalColor, u_color.a);
    }
      else if (u_volume_type == 1) {
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
        int N = int((tExit - tEntry) / dt);

        float t = tEntry + 0.5 * dt;

        // Optical thickness
        float thickness = 0.0;
        vec3 L = vec3(0.0); 

        for (int i = 0; i < N; ++i)
        {
            vec3 point = rayOriginLoc + t * rayDirLoc; 
            float absorption_coefficient = getAbsorption(point);

            thickness += absorption_coefficient * dt;
            float transmittance = exp(- thickness);

            // Emission contribution
            vec3 Le = u_color.rgb;
            L += absorption_coefficient * Le * transmittance * dt;
            
            t += dt;
        }     

        // Final color
        float transmittance_background = exp(- thickness);
        vec3 background = u_background_color.rgb;

        vec3 finalColor = L + background * transmittance_background;

        FragColor = vec4(finalColor, u_color.a);
    } else if (u_volume_type == 2) {
        // VDB-based volume rendering

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
        int N = int((tExit - tEntry) / dt);

        float t = tEntry + 0.5 * dt;

        // Optical thickness
        float thickness = 0.0;
        vec3 L = vec3(0.0); 

        for (int i = 0; i < N; ++i)
        {
            vec3 point = rayOriginLoc + t * rayDirLoc; 
            
            // Map from bounding box local space to texture space [0, 1]
            vec3 pointTex = (point - u_box_min) / (u_box_max - u_box_min);
            
            // Sample the 3D texture (GL_R8 auto-normalizes to [0,1])
            float density = texture(u_texture, pointTex).r;
            
            // Scale by absorption coefficient to control opacity
            // Multiply by large factor since GL_R8 normalizes 255 to 1.0
            float absorption_coefficient = density;

            thickness += absorption_coefficient * dt;
            float transmittance = exp(- thickness);

            // Emission contribution
            vec3 Le = u_color.rgb;
            L += absorption_coefficient * Le * transmittance * dt;
            
            t += dt;
        }

        // Final color
        float transmittance_background = exp(- thickness);
        vec3 background = u_background_color.rgb;

        vec3 finalColor = L + background * transmittance_background;

        // Discard if almost fully transparent
        if (transmittance_background > 0.99) {
            discard;
        }
        
        FragColor = vec4(finalColor, u_color.a);
    }
}
