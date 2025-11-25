#pragma once
#include <string>
#include <vector>
#include <glm/glm.hpp>

class VolumeDICOMLoader {
public:
    bool loadSeries(const std::string& folder);

    int width = 0;
    int height = 0;
    int depth = 0;

    float sliceSpacing = 1.0f;
    glm::vec3 voxelSpacing = glm::vec3(1.0f);

    // final raw volume: float intensities in [0..1]
    std::vector<float> volume;

    // sample 3D point in worldspace mm
    float sampleValue(const glm::vec3& p) const;

    glm::vec3 physMin;
    glm::vec3 physMax;

private:
    bool loadSortedDicomFiles(const std::string& folder, std::vector<std::string>& files);
};
