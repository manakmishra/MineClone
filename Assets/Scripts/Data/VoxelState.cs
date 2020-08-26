[System.Serializable]
public class VoxelState
{
    public int id;
    private byte _light;
    public byte light
    {
        get { return _light; }
        set { _light = value; }
    }

    public VoxelState(int _id)
    {
        id = _id;
        light = 0;
    }

    public float lightAsFloat
    {
        get { return (float)light * VoxelData.lightUnit; }
    }

    public BlockType properties
    {
        get { return World.Instance.blockTypes[id]; }
    }
}
