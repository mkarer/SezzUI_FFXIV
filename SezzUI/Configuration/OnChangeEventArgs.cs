﻿using System;

namespace SezzUI.Configuration;

public delegate void ConfigValueChangeEventHandler(PluginConfigObject sender, OnChangeBaseArgs args);

public enum ChangeType
{
	None = 0,
	ListAdd = 1,
	ListRemove = 2
}

public class OnChangeBaseArgs : EventArgs
{
	public string PropertyName { get; }
	public ChangeType ChangeType { get; }

	public OnChangeBaseArgs(string keyName, ChangeType type = ChangeType.None)
	{
		PropertyName = keyName;
		ChangeType = type;
	}
}

public class OnChangeEventArgs<T> : OnChangeBaseArgs
{
	public T Value { get; }

	public OnChangeEventArgs(string keyName, T value, ChangeType type = ChangeType.None) : base(keyName, type)
	{
		Value = value;
	}
}

public interface IOnChangeEventArgs
{
	public event ConfigValueChangeEventHandler? ValueChangeEvent;

	public void OnValueChanged(OnChangeBaseArgs e);
}