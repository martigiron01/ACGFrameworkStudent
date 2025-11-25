#include "VolumeDICOMLoader.h"
#include <gdcmImageReader.h>
#include <gdcmDirectory.h>
#include <gdcmIPPSorter.h>
#include <gdcmAttribute.h>
#include <algorithm>
#include <iostream>

bool VolumeDICOMLoader::loadSortedDicomFiles(const std::string& folder,
                                             std::vector<std::string>& files)
{
    gdcm::Directory dir;
    dir.Load(folder.c_str(), true);

    auto filenames = dir.GetFilenames();
    if (filenames.empty()) return false;

    gdcm::IPPSorter sorter;
    sorter.SetComputeZSpacing(true);

    if (!sorter.Sort(filenames)) return false;

    files = sorter.GetFilenames();
    return true;
}

bool VolumeDICOMLoader::loadSeries(const std::string& folder)
{
    std::vector<std::string> files;
    if (!loadSortedDicomFiles(folder, files))
        return false;

    depth = (int)files.size();
    if (depth == 0) return false;

    gdcm::ImageReader reader;
    reader.SetFileName(files[0].c_str());
    if (!reader.Read()) return false;

    const gdcm::Image& firstImg = reader.GetImage();
    width  = firstImg.GetDimension(0);
    height = firstImg.GetDimension(1);

    double spacing[3];
    firstImg.GetSpacing(spacing);
    voxelSpacing = glm::vec3(spacing[0], spacing[1], spacing[2]);

    double origin[3];
    firstImg.GetOrigin(origin);

    physMin = glm::vec3(origin[0], origin[1], origin[2]);

    sliceSpacing = spacing[2];

    volume.resize(width * height * depth);

    for (int s = 0; s < depth; s++)
    {
        gdcm::ImageReader r;
        r.SetFileName(files[s].c_str());
        r.Read();

        const gdcm::Image& img = r.GetImage();

        std::vector<char> buffer(img.GetBufferLength());
        img.GetBuffer(&buffer[0]);

        // 16-bit short pixels
        const int16_t* px = reinterpret_cast<const int16_t*>(&buffer[0]);

        int sliceOffset = s * width * height;
        for (int i = 0; i < width * height; i++)
        {
            // normalize to 0..1 for texture
            float v = (float)px[i];
            v = (v + 1024.0f) / 4096.0f; // adjust for CT, tweak if needed
            v = glm::clamp(v, 0.0f, 1.0f);
            volume[sliceOffset + i] = v;
        }
    }

    physMax = physMin + glm::vec3(
        voxelSpacing.x * width,
        voxelSpacing.y * height,
        voxelSpacing.z * depth
    );

    return true;
}

float VolumeDICOMLoader::sampleValue(const glm::vec3& p) const
{
    glm::vec3 rel = (p - physMin) /
        glm::vec3(voxelSpacing.x, voxelSpacing.y, voxelSpacing.z);

    float fx = rel.x;
    float fy = rel.y;
    float fz = rel.z;

    int x0 = (int)floor(fx);
    int y0 = (int)floor(fy);
    int z0 = (int)floor(fz);

    if (x0 < 0 || y0 < 0 || z0 < 0 ||
        x0 >= width - 1 || y0 >= height - 1 || z0 >= depth - 1)
        return 0.0f;

    float dx = fx - x0;
    float dy = fy - y0;
    float dz = fz - z0;

    auto idx = [&](int x, int y, int z) {
        return x + y * width + z * width * height;
    };

    float c000 = volume[idx(x0, y0, z0)];
    float c100 = volume[idx(x0+1,y0,z0)];
    float c010 = volume[idx(x0,y0+1,z0)];
    float c110 = volume[idx(x0+1,y0+1,z0)];
    float c001 = volume[idx(x0,y0,z0+1)];
    float c101 = volume[idx(x0+1,y0,z0+1)];
    float c011 = volume[idx(x0,y0+1,z0+1)];
    float c111 = volume[idx(x0+1,y0+1,z0+1)];

    float c00 = c000*(1-dx)+c100*dx;
    float c01 = c001*(1-dx)+c101*dx;
    float c10 = c010*(1-dx)+c110*dx;
    float c11 = c011*(1-dx)+c111*dx;

    float c0 = c00*(1-dy)+c10*dy;
    float c1 = c01*(1-dy)+c11*dy;

    return c0*(1-dz)+c1*dz;
}
