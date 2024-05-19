﻿using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.Common;
using LofiMixer.Components;

namespace LofiMixer.ViewModels;

public sealed class MusicViewModel : ObservableObject, IMutexSelectable
{
    public MusicViewModel(Uri musicFile, MutexSelector? mutexSelector)
    {
        _musicUri = musicFile;
        _mutexSelector = mutexSelector;
    }

    public string MusicName => Path.GetFileNameWithoutExtension(MusicUri.LocalPath ?? string.Empty);

    public Uri MusicUri
    {
        get => _musicUri;
        set => SetProperty(ref _musicUri, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _mutexSelector.GetThen(m =>
            {
                if (value)
                {
                    this.MutexSelect(m);
                }
                else
                {
                    this.MutexUnselect(m);
                }
            }).Else(() =>
            {
                SetProperty(ref _isSelected, value);
            });

            if (_isSelected)
            {
                Play();
            }
        }
    }

    bool IMutexSelectable.IsMutexSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value, nameof(IsSelected));
    }

    public void Play()
    {
        App.Current.ServiceProvider.GetServiceThen<IMusicPlayer>(x =>
        {
            x.Play(this);
        });
    }

    #region NonPublic
    private readonly Option<MutexSelector> _mutexSelector;
    private Uri _musicUri;
    private bool _isSelected;
    #endregion
}
