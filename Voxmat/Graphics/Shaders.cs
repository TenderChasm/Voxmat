using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxmat.Graphics
{
    public static class Shaders
    {
        public static string vertexCode = @"#version 330 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec4 aColor;
            layout (location = 2) in vec3 aNormal;

            out vec4 Color;
            out vec3 Normal;
            
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            

            void main()
            {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                Color = aColor;
                Normal = mat3(transpose(inverse(model))) * aNormal;
            }";

        public static string fragmentCode = @"#version 330 core
            out vec4 FragColor;
              
            in vec4 Color;
            in vec3 Normal;

            void main()
            {
                vec3 dirLight = vec3(1, 1, 1);
                float ambient = 0.2;
                float diff = max(dot(Normal, dirLight), 0.0);
                vec3 res = (diff + ambient) * Color.xyz;
                FragColor = vec4(res, 1.0);
            }";

        public static string vertexCodeOutline = @"#version 330 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec4 aColor;
            layout (location = 2) in vec3 aNormal;

            out vec4 Color;
            out vec3 Normal;
            
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            

            void main()
            {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                Color = vec4(188 / 255.0, 220 / 255.0 ,244 / 255.0 ,1.0);
                Normal = aNormal;
            }";

        public static string fragmentCodeOutline = @"#version 330 core
            out vec4 FragColor;
              
            in vec4 Color;
            in vec3 Normal;

            void main()
            {
                FragColor = Color;
            }";

    }
}
