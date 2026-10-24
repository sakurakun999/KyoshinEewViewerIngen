using KyoshinEewViewer.Services.ExternalPublishers.Axis.ApiModels.Message;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace KyoshinEewViewer.Services.ExternalPublishers.Axis;

/// <summary>
/// AXIS の EarthquakeMessage (JSON DTO) を気象庁防災情報XML (JMAXML) 互換の XML バイト列へ変換するコンバータ
/// </summary>
public static class AxisEarthquakeToXmlConverter
{
	private const string JmaXmlNamespace = "http://xml.kishou.go.jp/jmaxml1/";
	private const string ElementBasisNamespace = "http://xml.kishou.go.jp/jmaxml1/elementBasis1/";

	public static byte[] ConvertToXml(EarthquakeMessage message)
	{
		ArgumentNullException.ThrowIfNull(message);

		using var ms = new MemoryStream();
		var settings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			Indent = false,
			OmitXmlDeclaration = false,
		};

		using (var writer = XmlWriter.Create(ms, settings))
		{
			writer.WriteStartDocument();

			// <Report xmlns="http://xml.kishou.go.jp/jmaxml1/">
			writer.WriteStartElement("Report", JmaXmlNamespace);
			writer.WriteAttributeString("xmlns", "jmx_eb", null, ElementBasisNamespace);

			// <Control>
			WriteControl(writer, message.Control);

			// <Head>
			WriteHead(writer, message.Head);

			// <Body>
			WriteBody(writer, message.Body);

			writer.WriteEndElement(); // </Report>
			writer.WriteEndDocument();
		}

		return ms.ToArray();
	}

	private static void WriteControl(XmlWriter writer, JmaXmlControl? control)
	{
		writer.WriteStartElement("Control", JmaXmlNamespace);
		if (control != null)
		{
			if (!string.IsNullOrEmpty(control.Title))
				writer.WriteElementString("Title", JmaXmlNamespace, control.Title);

			if (control.DateTime != default)
				writer.WriteElementString("DateTime", JmaXmlNamespace, control.DateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));

			if (!string.IsNullOrEmpty(control.Status))
				writer.WriteElementString("Status", JmaXmlNamespace, control.Status);

			if (!string.IsNullOrEmpty(control.EditorialOffice))
				writer.WriteElementString("EditorialOffice", JmaXmlNamespace, control.EditorialOffice);

			if (!string.IsNullOrEmpty(control.PublishingOffice))
				writer.WriteElementString("PublishingOffice", JmaXmlNamespace, control.PublishingOffice);
		}
		writer.WriteEndElement(); // </Control>
	}

	private static void WriteHead(XmlWriter writer, JmaXmlHead? head)
	{
		writer.WriteStartElement("Head", JmaXmlNamespace);
		if (head != null)
		{
			if (!string.IsNullOrEmpty(head.Title))
				writer.WriteElementString("Title", JmaXmlNamespace, head.Title);

			if (head.ReportDateTime != default)
				writer.WriteElementString("ReportDateTime", JmaXmlNamespace, head.ReportDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));

			if (head.TargetDateTime != default)
				writer.WriteElementString("TargetDateTime", JmaXmlNamespace, head.TargetDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));

			if (!string.IsNullOrEmpty(head.EventID))
				writer.WriteElementString("EventID", JmaXmlNamespace, head.EventID);

			if (!string.IsNullOrEmpty(head.InfoType))
				writer.WriteElementString("InfoType", JmaXmlNamespace, head.InfoType);

			if (!string.IsNullOrEmpty(head.Serial))
				writer.WriteElementString("Serial", JmaXmlNamespace, head.Serial);

			if (!string.IsNullOrEmpty(head.InfoKind))
				writer.WriteElementString("InfoKind", JmaXmlNamespace, head.InfoKind);

			if (!string.IsNullOrEmpty(head.InfoKindVersion))
				writer.WriteElementString("InfoKindVersion", JmaXmlNamespace, head.InfoKindVersion);

			if (head.Headline != null && !string.IsNullOrEmpty(head.Headline.Text))
			{
				writer.WriteStartElement("Headline", JmaXmlNamespace);
				writer.WriteElementString("Text", JmaXmlNamespace, head.Headline.Text);
				writer.WriteEndElement();
			}
		}
		writer.WriteEndElement(); // </Head>
	}

	private static void WriteBody(XmlWriter writer, EarthquakeBody? body)
	{
		writer.WriteStartElement("Body", JmaXmlNamespace);
		if (body != null)
		{
			// <Earthquake>
			if (body.Earthquake != null)
			{
				foreach (var eq in body.Earthquake)
				{
					if (eq == null) continue;
					writer.WriteStartElement("Earthquake", JmaXmlNamespace);

					if (eq.OriginTime != default)
						writer.WriteElementString("OriginTime", JmaXmlNamespace, eq.OriginTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));

					if (eq.ArrivalTime != default)
						writer.WriteElementString("ArrivalTime", JmaXmlNamespace, eq.ArrivalTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));

					if (!string.IsNullOrEmpty(eq.Condition))
						writer.WriteElementString("Condition", JmaXmlNamespace, eq.Condition);

					if (eq.Hypocenter?.Area != null)
					{
						writer.WriteStartElement("Hypocenter", JmaXmlNamespace);
						writer.WriteStartElement("Area", JmaXmlNamespace);

						if (!string.IsNullOrEmpty(eq.Hypocenter.Area.Name))
							writer.WriteElementString("Name", JmaXmlNamespace, eq.Hypocenter.Area.Name);

						if (eq.Hypocenter.Area.Code != null)
						{
							writer.WriteStartElement("Code", JmaXmlNamespace);
							if (!string.IsNullOrEmpty(eq.Hypocenter.Area.Code.Type))
								writer.WriteAttributeString("type", eq.Hypocenter.Area.Code.Type);
							writer.WriteString(eq.Hypocenter.Area.Code.ValueOf ?? "");
							writer.WriteEndElement();
						}

						if (eq.Hypocenter.Area.Coordinate != null)
						{
							foreach (var coord in eq.Hypocenter.Area.Coordinate)
							{
								if (coord == null) continue;
								writer.WriteStartElement("jmx_eb", "Coordinate", ElementBasisNamespace);
								writer.WriteAttributeString("description", coord.Description ?? "");
								if (!string.IsNullOrEmpty(coord.Type))
									writer.WriteAttributeString("type", coord.Type);
								writer.WriteString(coord.ValueOf ?? "");
								writer.WriteEndElement();
							}
						}

						writer.WriteEndElement(); // </Area>
						writer.WriteEndElement(); // </Hypocenter>
					}

					if (eq.Magnitude != null)
					{
						foreach (var mag in eq.Magnitude)
						{
							if (mag == null) continue;
							writer.WriteStartElement("jmx_eb", "Magnitude", ElementBasisNamespace);
							writer.WriteAttributeString("type", !string.IsNullOrEmpty(mag.Type) ? mag.Type : "Mj");
							if (!string.IsNullOrEmpty(mag.Description))
								writer.WriteAttributeString("description", mag.Description);
							writer.WriteString(mag.ValueOf ?? "");
							writer.WriteEndElement();
						}
					}

					writer.WriteEndElement(); // </Earthquake>
				}
			}

			// <Intensity>
			if (body.Intensity?.Observation != null)
			{
				writer.WriteStartElement("Intensity", JmaXmlNamespace);
				writer.WriteStartElement("Observation", JmaXmlNamespace);

				var obs = body.Intensity.Observation;
				if (!string.IsNullOrEmpty(obs.MaxInt))
					writer.WriteElementString("MaxInt", JmaXmlNamespace, obs.MaxInt);
				if (!string.IsNullOrEmpty(obs.MaxLgInt))
					writer.WriteElementString("MaxLgInt", JmaXmlNamespace, obs.MaxLgInt);

				if (obs.Pref != null)
				{
					foreach (var pref in obs.Pref)
					{
						if (pref == null) continue;
						writer.WriteStartElement("Pref", JmaXmlNamespace);
						if (!string.IsNullOrEmpty(pref.Name))
							writer.WriteElementString("Name", JmaXmlNamespace, pref.Name);
						if (!string.IsNullOrEmpty(pref.Code))
							writer.WriteElementString("Code", JmaXmlNamespace, pref.Code);
						if (!string.IsNullOrEmpty(pref.MaxInt))
							writer.WriteElementString("MaxInt", JmaXmlNamespace, pref.MaxInt);
						if (!string.IsNullOrEmpty(pref.MaxLgInt))
							writer.WriteElementString("MaxLgInt", JmaXmlNamespace, pref.MaxLgInt);

						if (pref.Area != null)
						{
							foreach (var area in pref.Area)
							{
								if (area == null) continue;
								writer.WriteStartElement("Area", JmaXmlNamespace);
								if (!string.IsNullOrEmpty(area.Name))
									writer.WriteElementString("Name", JmaXmlNamespace, area.Name);
								if (!string.IsNullOrEmpty(area.Code))
									writer.WriteElementString("Code", JmaXmlNamespace, area.Code);
								if (!string.IsNullOrEmpty(area.MaxInt))
									writer.WriteElementString("MaxInt", JmaXmlNamespace, area.MaxInt);
								if (!string.IsNullOrEmpty(area.MaxLgInt))
									writer.WriteElementString("MaxLgInt", JmaXmlNamespace, area.MaxLgInt);

								if (area.City != null)
								{
									foreach (var city in area.City)
									{
										if (city == null) continue;
										writer.WriteStartElement("City", JmaXmlNamespace);
										if (!string.IsNullOrEmpty(city.Name))
											writer.WriteElementString("Name", JmaXmlNamespace, city.Name);
										if (!string.IsNullOrEmpty(city.Code))
											writer.WriteElementString("Code", JmaXmlNamespace, city.Code);
										if (!string.IsNullOrEmpty(city.MaxInt))
											writer.WriteElementString("MaxInt", JmaXmlNamespace, city.MaxInt);
										if (!string.IsNullOrEmpty(city.MaxLgInt))
											writer.WriteElementString("MaxLgInt", JmaXmlNamespace, city.MaxLgInt);

										if (city.IntensityStation != null)
										{
											foreach (var st in city.IntensityStation)
											{
												if (st == null) continue;
												writer.WriteStartElement("IntensityStation", JmaXmlNamespace);
												if (!string.IsNullOrEmpty(st.Name))
													writer.WriteElementString("Name", JmaXmlNamespace, st.Name);
												if (!string.IsNullOrEmpty(st.Code))
													writer.WriteElementString("Code", JmaXmlNamespace, st.Code);
												if (!string.IsNullOrEmpty(st.Int))
													writer.WriteElementString("Int", JmaXmlNamespace, st.Int);
												writer.WriteEndElement(); // </IntensityStation>
											}
										}

										writer.WriteEndElement(); // </City>
									}
								}

								writer.WriteEndElement(); // </Area>
							}
						}

						writer.WriteEndElement(); // </Pref>
					}
				}

				writer.WriteEndElement(); // </Observation>
				writer.WriteEndElement(); // </Intensity>
			}

			// <Comments>
			if (body.Comments != null)
			{
				writer.WriteStartElement("Comments", JmaXmlNamespace);

				if (body.Comments.ForecastComment != null)
				{
					writer.WriteStartElement("ForecastComment", JmaXmlNamespace);
					if (!string.IsNullOrEmpty(body.Comments.ForecastComment.Text))
						writer.WriteElementString("Text", JmaXmlNamespace, body.Comments.ForecastComment.Text);
					if (!string.IsNullOrEmpty(body.Comments.ForecastComment.Code))
						writer.WriteElementString("Code", JmaXmlNamespace, body.Comments.ForecastComment.Code);
					writer.WriteEndElement();
				}

				if (body.Comments.VarComment != null)
				{
					writer.WriteStartElement("VarComment", JmaXmlNamespace);
					if (!string.IsNullOrEmpty(body.Comments.VarComment.Text))
						writer.WriteElementString("Text", JmaXmlNamespace, body.Comments.VarComment.Text);
					if (!string.IsNullOrEmpty(body.Comments.VarComment.Code))
						writer.WriteElementString("Code", JmaXmlNamespace, body.Comments.VarComment.Code);
					writer.WriteEndElement();
				}

				if (body.Comments.WarningComment != null)
				{
					writer.WriteStartElement("WarningComment", JmaXmlNamespace);
					if (!string.IsNullOrEmpty(body.Comments.WarningComment.Text))
						writer.WriteElementString("Text", JmaXmlNamespace, body.Comments.WarningComment.Text);
					if (!string.IsNullOrEmpty(body.Comments.WarningComment.Code))
						writer.WriteElementString("Code", JmaXmlNamespace, body.Comments.WarningComment.Code);
					writer.WriteEndElement();
				}

				if (!string.IsNullOrEmpty(body.Comments.FreeFormComment))
					writer.WriteElementString("FreeFormComment", JmaXmlNamespace, body.Comments.FreeFormComment);

				writer.WriteEndElement(); // </Comments>
			}
		}
		writer.WriteEndElement(); // </Body>
	}
}
