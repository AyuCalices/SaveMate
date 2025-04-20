namespace SaveMate.Runtime.Core.SaveStrategies.Integrity
{
    internal interface IIntegrityStrategy
    {
        string ComputeChecksum(byte[] data);
    }
}
