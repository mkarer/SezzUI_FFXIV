using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using SezzUI.Configuration;

namespace SezzUI.Interface;

public abstract class HudElement : IDisposable
{
	protected AnchorablePluginConfigObject _config;
	public AnchorablePluginConfigObject GetConfig() => _config;

	public string ID => _config.ID;

	public HudElement(AnchorablePluginConfigObject config)
	{
		_config = config;
	}

	public abstract void Draw(Vector2 origin);

	~HudElement()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		InternalDispose();
	}

	protected virtual void InternalDispose()
	{
		// override
	}
}

public interface IHudElementWithActor
{
	public GameObject? Actor { get; set; }
}

public interface IHudElementWithAnchorableParent
{
	public AnchorablePluginConfigObject? ParentConfig { get; set; }
}

public interface IHudElementWithMouseOver
{
	public void StopMouseover();
}

public interface IHudElementWithPreview
{
	public void StopPreview();
}