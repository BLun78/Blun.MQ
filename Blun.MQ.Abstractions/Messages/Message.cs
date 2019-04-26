using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Blun.MQ.Collections;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    [DataContract()]
    public class Message : INotifyPropertyChanged
    {
        private IDictionary<string, string> _headers;
        private string _messageId;
        private string _body;

        public Message()
        {
            if (Headers != null) return;
            var observableDictionary = new ObservableDictionary<string, string>(StringComparer.InvariantCulture);
            observableDictionary.CollectionChanged += OnObservableDictionaryCollectionChanged;
            observableDictionary.PropertyChanged += ObservableDictionaryOnPropertyChanged;
            this.Headers = observableDictionary;
        }


        internal Message(string messageId) : this()
        {
            this.MessageId = messageId;
            var observableDictionary = new ObservableDictionary<string, string>(StringComparer.InvariantCulture);
            observableDictionary.CollectionChanged += OnObservableDictionaryCollectionChanged;
            observableDictionary.PropertyChanged += ObservableDictionaryOnPropertyChanged;
            this.Headers = observableDictionary;
        }

        internal Message(string messageId, IDictionary<string, string> headers) : this(messageId)
        {
            var observableDictionary =
                new ObservableDictionary<string, string>(headers, StringComparer.InvariantCulture);
            observableDictionary.CollectionChanged += OnObservableDictionaryCollectionChanged;
            observableDictionary.PropertyChanged += ObservableDictionaryOnPropertyChanged;
            this.Headers = observableDictionary;
        }


        internal Message(string messageId, IDictionary<string, string> headers, string body) : this(messageId, headers)
        {
            this.Body = body;
        }

        [DataMember]
        public string MessageId
        {
            get => _messageId;
            internal set
            {
                if (value == _messageId) return;
                _messageId = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public IDictionary<string, string> Headers
        {
            get => _headers;
            internal set
            {
                if (Equals(value, _headers)) return;
                _headers = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string Body
        {
            get => _body;
            internal set
            {
                if (value == _body) return;
                _body = value;
                OnPropertyChanged();
            }
        }

        [IgnoreDataMember] public int MessageSize { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnObservableDictionaryCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Headers)));
        }

        [NotifyPropertyChangedInvocator]
        private void ObservableDictionaryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}