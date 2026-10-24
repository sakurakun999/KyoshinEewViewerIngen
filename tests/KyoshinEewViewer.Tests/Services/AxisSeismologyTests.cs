using KyoshinEewViewer.JmaXmlParser;
using KyoshinEewViewer.Series.Earthquake.Models;
using KyoshinEewViewer.Services.ExternalPublishers.Axis;
using KyoshinEewViewer.Services.ExternalPublishers.Axis.ApiModels.Message;
using KyoshinMonitorLib;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace KyoshinEewViewer.Tests.Services;

public class AxisSeismologyTests
{
	private const string SampleHypoAndIntJson = """
	{
		"Control": {
			"Title": "震源・震度に関する情報",
			"DateTime": "2026-10-25T00:15:00+09:00",
			"Status": "通常",
			"EditorialOffice": "気象庁本庁",
			"PublishingOffice": "気象庁"
		},
		"Head": {
			"Title": "震源・震度に関する情報",
			"ReportDateTime": "2026-10-25T00:15:00+09:00",
			"TargetDateTime": "2026-10-25T00:15:00+09:00",
			"EventID": "20261025001500",
			"InfoType": "発表",
			"Serial": "1",
			"InfoKind": "同一報",
			"InfoKindVersion": "1.0_1",
			"Headline": {
				"Text": "２５日００時１２分頃地震がありました。"
			}
		},
		"Body": {
			"Earthquake": [
				{
					"OriginTime": "2026-10-25T00:12:00+09:00",
					"ArrivalTime": "2026-10-25T00:12:00+09:00",
					"Hypocenter": {
						"Area": {
							"Name": "石川県能登地方",
							"Code": {
								"type_": "震央地名",
								"valueOf_": "381"
							},
							"Coordinate": [
								{
									"type_": "震源位置",
									"description": "北緯３７．５度　東経１３７．２度　深さ　１０ｋｍ",
									"valueOf_": "+37.5+137.2-10000/"
								}
							]
						}
					},
					"Magnitude": [
						{
							"type_": "Mj",
							"description": "Ｍ５．０",
							"valueOf_": "5.0"
						}
					]
				}
			],
			"Intensity": {
				"Observation": {
					"MaxInt": "4",
					"Pref": [
						{
							"Name": "石川県",
							"Code": "17",
							"MaxInt": "4",
							"Area": [
								{
									"Name": "石川県能登",
									"Code": "381",
									"MaxInt": "4",
									"City": [
										{
											"Name": "輪島市",
											"Code": "1720400",
											"MaxInt": "4",
											"IntensityStation": [
												{
													"Name": "輪島市鳳至町",
													"Code": "1720400",
													"Int": "4"
												}
											]
										}
									]
								}
							]
						}
					]
				}
			},
			"Comments": {
				"ForecastComment": {
					"Text": "この地震による津波の心配はありません。",
					"Code": "0101"
				}
			}
		},
		"uuid_": "test-uuid-12345"
	}
	""";

	private const string SampleShindoSokuhouJson = """
	{
		"Control": {
			"Title": "震度速報",
			"DateTime": "2026-10-25T00:13:00+09:00",
			"Status": "通常",
			"EditorialOffice": "気象庁本庁",
			"PublishingOffice": "気象庁"
		},
		"Head": {
			"Title": "震度速報",
			"ReportDateTime": "2026-10-25T00:13:00+09:00",
			"TargetDateTime": "2026-10-25T00:12:00+09:00",
			"EventID": "20261025001200",
			"InfoType": "発表",
			"Serial": "1",
			"Headline": {
				"Text": "２５日００時１２分頃地震による強い揺れを感じました。"
			}
		},
		"Body": {
			"Intensity": {
				"Observation": {
					"MaxInt": "4",
					"Pref": [
						{
							"Name": "石川県",
							"Code": "17",
							"MaxInt": "4",
							"Area": [
								{
									"Name": "石川県能登",
									"Code": "381",
									"MaxInt": "4"
								}
							]
						}
					]
				}
			}
		},
		"uuid_": "sokuhou-uuid-67890"
	}
	""";

	[Fact]
	public void CanDeserializeEarthquakeMessageFromJson()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var message = JsonSerializer.Deserialize<EarthquakeMessage>(SampleHypoAndIntJson, options);

		Assert.NotNull(message);
		Assert.NotNull(message.Control);
		Assert.Equal("震源・震度に関する情報", message.Control.Title);
		Assert.Equal("test-uuid-12345", message.Uuid);

		Assert.NotNull(message.Head);
		Assert.Equal("20261025001500", message.Head.EventID);

		Assert.NotNull(message.Body);
		Assert.NotNull(message.Body.Earthquake);
		Assert.Single(message.Body.Earthquake);

		var eq = message.Body.Earthquake[0];
		Assert.Equal("石川県能登地方", eq.Hypocenter?.Area?.Name);
		Assert.Equal("381", eq.Hypocenter?.Area?.Code?.ValueOf);
		Assert.Equal("+37.5+137.2-10000/", eq.Hypocenter?.Area?.Coordinate?[0].ValueOf);
		Assert.Equal("5.0", eq.Magnitude?[0].ValueOf);

		Assert.NotNull(message.Body.Intensity?.Observation);
		Assert.Equal("4", message.Body.Intensity.Observation.MaxInt);
	}

	[Fact]
	public void CanConvertToXmlAndParseWithJmaXmlDocument()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var message = JsonSerializer.Deserialize<EarthquakeMessage>(SampleHypoAndIntJson, options);
		Assert.NotNull(message);

		var xmlBytes = AxisEarthquakeToXmlConverter.ConvertToXml(message);
		Assert.NotEmpty(xmlBytes);

		using var doc = new JmaXmlDocument(xmlBytes);
		Assert.Equal("震源・震度に関する情報", doc.Control.Title);
		Assert.Equal("20261025001500", doc.Head.EventId);
		Assert.Equal("発表", doc.Head.InfoType);

		Assert.NotNull(doc.EarthquakeBody.Earthquake);
		var eqData = doc.EarthquakeBody.Earthquake.Value;
		Assert.Equal("石川県能登地方", eqData.Hypocenter.Area.Name);
		Assert.Equal("381", eqData.Hypocenter.Area.Code);
		Assert.True(eqData.Magnitude.TryGetFloatValue(out var mag));
		Assert.Equal(5.0f, mag);

		Assert.NotNull(doc.EarthquakeBody.Intensity?.Observation);
		var obs = doc.EarthquakeBody.Intensity.Value.Observation.Value;
		Assert.Equal("4", obs.MaxInt);
		Assert.Single(obs.Prefs);
	}

	[Fact]
	public async Task CanCreateEarthquakeInformationFragmentFromAxisTelegram()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var message = JsonSerializer.Deserialize<EarthquakeMessage>(SampleHypoAndIntJson, options);
		Assert.NotNull(message);

		var xmlBytes = AxisEarthquakeToXmlConverter.ConvertToXml(message);
		var telegram = new AxisTelegram(message, xmlBytes);

		Assert.Equal("axis_test-uuid-12345", telegram.Key);
		Assert.Equal("震源・震度に関する情報", telegram.Title);

		await using var stream = await telegram.GetBodyAsync();
		using var doc = new JmaXmlDocument(stream);

		var fragment = EarthquakeInformationFragment.CreateFromJmxXmlDocument(telegram, doc);
		Assert.NotNull(fragment);
		Assert.IsType<HypocenterAndIntensityInformationFragment>(fragment);

		var hiFragment = (HypocenterAndIntensityInformationFragment)fragment;
		Assert.Equal("石川県能登地方", hiFragment.Place);
		Assert.Equal(5.0f, hiFragment.Magnitude);
		Assert.Equal(10, hiFragment.Depth);
		Assert.Equal(JmaIntensity.Int4, hiFragment.MaxIntensity);
		Assert.NotNull(hiFragment.Location);
		Assert.InRange(hiFragment.Location.Latitude, 37.4f, 37.6f);
		Assert.InRange(hiFragment.Location.Longitude, 137.1f, 137.3f);
	}

	[Fact]
	public async Task CanHandleShindoSokuhouMessage()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var message = JsonSerializer.Deserialize<EarthquakeMessage>(SampleShindoSokuhouJson, options);
		Assert.NotNull(message);

		var xmlBytes = AxisEarthquakeToXmlConverter.ConvertToXml(message);
		var telegram = new AxisTelegram(message, xmlBytes);

		await using var stream = await telegram.GetBodyAsync();
		using var doc = new JmaXmlDocument(stream);

		Assert.Equal("震度速報", doc.Control.Title);

		var fragment = EarthquakeInformationFragment.CreateFromJmxXmlDocument(telegram, doc);
		Assert.NotNull(fragment);
		Assert.IsType<IntensityInformationFragment>(fragment);

		var intFragment = (IntensityInformationFragment)fragment;
		Assert.Equal("石川県能登", intFragment.Place);
		Assert.Equal(JmaIntensity.Int4, intFragment.MaxIntensity);
	}

	[Fact]
	public void HandlesEmptyOrNullFieldsGracefully()
	{
		var emptyMessage = new EarthquakeMessage
		{
			Control = new JmaXmlControl { Title = "震度速報" },
			Head = new JmaXmlHead { EventID = "20261025000000" },
			Body = new EarthquakeBody()
		};

		var xmlBytes = AxisEarthquakeToXmlConverter.ConvertToXml(emptyMessage);
		Assert.NotEmpty(xmlBytes);

		using var doc = new JmaXmlDocument(xmlBytes);
		Assert.Equal("震度速報", doc.Control.Title);
		Assert.Equal("20261025000000", doc.Head.EventId);
	}
}
