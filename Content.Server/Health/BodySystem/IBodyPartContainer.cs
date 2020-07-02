﻿using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.Health.BodySystem.Surgery.Data;

namespace Content.Server.BodySystem
{

    /// <summary>
    ///     Making a class inherit from this interface allows you to do many things with it in the <see cref="SurgeryData"/> class. This includes passing
    ///     it as an argument to a <see cref="SurgeryData.SurgeryAction"/> delegate, as to later typecast it back to the original class type. Every BodyPart also needs an
    ///     IBodyPartContainer to be its parent (i.e. the BodyManagerComponent holds many BodyParts, each of which have an upward reference to it).
    /// </summary>

    public interface IBodyPartContainer
    {

    }
}
