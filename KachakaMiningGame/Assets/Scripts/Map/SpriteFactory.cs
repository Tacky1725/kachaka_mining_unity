using UnityEngine;

public static class SpriteFactory
{
    public static Sprite CreateSolidSprite(string name, Color color)
    {
        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        FillTexture(texture, color);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }

    public static Sprite CreateCircleSprite(string name, Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? color : Color.clear);
            }
        }

        texture.name = name;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static Sprite CreateTriangleSprite(string name, Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = Color.clear;

        for (int y = 0; y < size; y++)
        {
            float rowWidth = Mathf.Lerp(size * 0.16f, size * 0.75f, y / (float)(size - 1));
            float minX = (size - rowWidth) * 0.5f;
            float maxX = (size + rowWidth) * 0.5f;

            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, x >= minX && x <= maxX ? color : clear);
            }
        }

        texture.name = name;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void FillTexture(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
