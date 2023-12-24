namespace BCnEncoder.Shared;

/// <summary>
/// 
/// </summary>
public readonly struct ProgressElement(int currentBlock, int totalBlocks)
{
	/// <summary>
	/// Current block being processed
	/// </summary>
	public int CurrentBlock { get; } = currentBlock;

	/// <summary>
	/// The total amount of blocks to be processed
	/// </summary>
	public int TotalBlocks { get; } = totalBlocks;

	/// <summary>
	/// Returns the progress percentage as a float from 0 to 1
	/// </summary>
	public float Percentage => CurrentBlock / (float) TotalBlocks;

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public override string ToString() =>
		$"{nameof(CurrentBlock)}: {CurrentBlock}, {nameof(TotalBlocks)}:" +
		$"{TotalBlocks}, {nameof(Percentage)}: {Percentage}";
}
