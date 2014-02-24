// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Data;
using EventStore.Core.Services;
using EventStore.Core.Tests.ClientAPI.Helpers;
using NUnit.Framework;
using ExpectedVersion = EventStore.ClientAPI.ExpectedVersion;
using StreamMetadata = EventStore.ClientAPI.StreamMetadata;

namespace EventStore.Core.Tests.ClientAPI
{
    [TestFixture, Category("LongRunning")]
    public class read_stream_events_with_unresolved_linkto : SpecificationWithMiniNode
    {
        private EventData[] _testEvents;

        protected override void When()
        {
            _conn.SetStreamMetadata(
                "$all", -1, StreamMetadata.Build().SetReadRole(SystemRoles.All),
                new UserCredentials(SystemUsers.Admin, SystemUsers.DefaultAdminPassword));

            _testEvents = Enumerable.Range(0, 20).Select(x => TestEvent.NewTestEvent(x.ToString())).ToArray();
            _conn.AppendToStream("stream", ExpectedVersion.EmptyStream, _testEvents);
            _conn.AppendToStream(
                "links", ExpectedVersion.EmptyStream,
                new EventData(
                    Guid.NewGuid(), EventStore.ClientAPI.Common.SystemEventTypes.LinkTo, false,
                    Encoding.UTF8.GetBytes("0@stream"), null));
            _conn.DeleteStream("stream", ExpectedVersion.Any);
        }

        [Test, Category("LongRunning")]
        public void ensure_deleted_stream()
        {
            var res = _conn.ReadStreamEventsForward("stream", 0, 100, false);
            Assert.AreEqual(SliceReadStatus.StreamNotFound, res.Status);
            Assert.AreEqual(0, res.Events.Length);
        }

        [Test, Category("LongRunning")]
        public void returns_unresolved_linkto()
        {
            var read = _conn.ReadStreamEventsForward("links", 0, 1, true);
            Assert.AreEqual(1, read.Events.Length);
            Assert.IsNull(read.Events[0].Event);
            Assert.IsNotNull(read.Events[0].Link);
        }
    }
}