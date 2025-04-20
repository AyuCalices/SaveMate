namespace SaveMate.Core.SaveStrategies.Integrity
{
    internal interface IIntegrityStrategy
    {
        string ComputeChecksum(byte[] data);
    }
}
