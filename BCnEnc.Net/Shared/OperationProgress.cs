using System;

namespace BCnEncoder.Shared;

/// <summary>
/// 
/// </summary>
public class OperationProgress(IProgress<ProgressElement> progress, int totalBlocks)
{
	private int processedBlocks;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="processedBlocks"></param>
	public void SetProcessedBlocks(int processedBlocks)
	{
		this.processedBlocks = processedBlocks;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="currentBlock"></param>
	public void Report(int currentBlock)
	{
		progress?.Report(new ProgressElement(processedBlocks + currentBlock, totalBlocks));
	}
}
