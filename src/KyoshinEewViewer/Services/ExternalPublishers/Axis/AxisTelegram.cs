using KyoshinEewViewer.Services.ExternalPublishers.Axis.ApiModels.Message;
using KyoshinEewViewer.Services.TelegramPublishers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KyoshinEewViewer.Services.ExternalPublishers.Axis;

/// <summary>
/// AXIS の jmx-seismology から受信した地震情報電文
/// </summary>
public class AxisTelegram : Telegram
{
	private byte[] XmlBytes { get; }

	public AxisTelegram(EarthquakeMessage message, byte[] xmlBytes)
		: base(
			key: "axis_" + (message.Uuid ?? (message.Head?.EventID + "_" + (message.Head?.Serial ?? "1"))),
			title: message.Control?.Title ?? "地震情報",
			rawId: message.Head?.EventID ?? "",
			arrivalTime: message.Control?.DateTime.DateTime ?? DateTime.Now
		)
	{
		XmlBytes = xmlBytes ?? throw new ArgumentNullException(nameof(xmlBytes));
	}

	public override Task<Stream> GetBodyAsync()
		=> Task.FromResult<Stream>(new MemoryStream(XmlBytes));

	public override void Cleanup()
	{
		// メモリ内保持のため特別なクリーンアップは不要
	}
}
