using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Markings;

// the better version of MarkingsSet
// This one should ensure that a set is valid. Dependency retrieval is
// probably not a good idea, and any dependency references should last
// only for the length of a call, and not the lifetime of the set itself.
//
// Compared to MarkingsSet, this should allow for server-side authority.
// Instead of sending the set over, we can instead just send the dictionary
// and build the set from there. We can also just send a list and rebuild
// the set without validating points (we're assuming that the server
// will always send the correct data here).

public sealed class MarkingSet
{
    /// <summary>
    ///     Every single marking in this set.
    /// </summary>
    /// <remarks>
    ///     The original version of MarkingSet preserved ordering across all
    ///     markings - this one should instead preserve ordering across all
    ///     categories, but not marking categories themselves. This is because
    ///     the layers that markings appear in are guaranteed to be in the correct
    ///     order. This is here to make lookups slightly faster, even if the n of
    ///     a marking set is relatively small, and to encapsulate another important
    ///     feature of markings, which is the limit of markings you can put on a
    ///     humanoid.
    /// </remarks>
    private Dictionary<MarkingCategories, List<Marking>> _markings = new();

    // why i didn't encapsulate this in the first place, i won't know

    /// <summary>
    ///     Marking points for each category.
    /// </summary>
    private Dictionary<MarkingCategories, MarkingPoints> _points = new();

    public List<Marking> this[MarkingCategories category] => _markings[category];

    public MarkingSet()
    {}

    /// <summary>
    ///     Construct a MarkingSet using a list of markings, and a points
    ///     dictionary. This will set up the points dictionary, and
    ///     process the list, truncating if necessary.
    /// </summary>
    /// <param name="markings"></param>
    /// <param name="pointsPrototype">The ID of the points dictionary prototype.</param>
    public MarkingSet(List<Marking> markings, string pointsPrototype)
    {

    }

    /// <summary>
    ///     Construct a MarkingSet using a dictionary of markings,
    ///     without validation.
    /// </summary>
    /// <param name="markings"></param>
    public MarkingSet(List<Marking> markings)
    {
    }

    /// <summary>
    ///     Construct a MarkingSet by cloning another set.
    /// </summary>
    /// <param name="other"></param>
    public MarkingSet(MarkingSet other)
    {
        _markings = new(other._markings);
        _points = new(other._points);
    }

    /// <summary>
    ///     Add a marking to the front of the category's list of markings.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="marking"></param>
    public void AddFront(MarkingCategories category, Marking marking)
    {
        if (_points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!_markings.TryGetValue(category, out var markings))
        {
            markings = new();
            _markings[category] = markings;
        }

        markings.Insert(0, marking);
    }

    /// <summary>
    ///     Add a marking to the back of the category's list of markings.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="marking"></param>
    public void AddBack(MarkingCategories category, Marking marking)
    {
        if (_points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!_markings.TryGetValue(category, out var markings))
        {
            markings = new();
            _markings[category] = markings;
        }


        markings.Add(marking);
    }

    public List<Marking> AddCategory(MarkingCategories category)
    {
        var markings = new List<Marking>();
        _markings.Add(category, markings);
        return markings;
    }

    public bool Remove(MarkingCategories category, string id)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        for (var i = 0; i < markings.Count; i++)
        {
            if (markings[i].MarkingId == id)
            {
                markings.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void Remove(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        // TODO: This should re-add points.
        markings.RemoveAt(idx);
    }

    public bool RemoveCategory(MarkingCategories category)
    {
        // TODO: This should re-add points.
        return _markings.Remove(category);
    }

    public int FindIndexOf(MarkingCategories category, string id)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return -1;
        }

        return markings.FindIndex(m => m.MarkingId == id);
    }

    public bool TryGetMarking(MarkingCategories category, string id, [NotNullWhen(true)] out Marking? marking)
    {
        marking = null;

        if (!_markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        foreach (var m in markings)
        {
            if (m.MarkingId == id)
            {
                marking = m;
                return true;
            }
        }

        return false;
    }

    public void ShiftRankUp(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx - 1 < 0)
        {
            return;
        }

        var temp = markings[idx - 1];
        markings[idx - 1] = markings[idx];
        markings[idx] = temp;
    }

    // Shifts up from the back (i.e., 2nd position from end)
    public void ShiftRankUpFromEnd(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankUp(category, markings.Count - idx - 1);
    }

    // Ditto, but the opposite direction.
    public void ShiftRankDown(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx + 1 >= markings.Count)
        {
            return;
        }

        var temp = markings[idx + 1];
        markings[idx + 1] = markings[idx];
        markings[idx] = temp;
    }

    // Ditto as above.
    public void ShiftRankDownFromEnd(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankDown(category, markings.Count - idx - 1);
    }

    public ForwardMarkingEnumerator GetForwardEnumerator()
    {
        var markings = new List<Marking>();
        foreach (var (_, list) in _markings)
        {
            markings.AddRange(list);
        }

        return new ForwardMarkingEnumerator(markings);
    }
}

public sealed class ForwardMarkingEnumerator : IEnumerable
{
    private List<Marking> _markings;

    public ForwardMarkingEnumerator(List<Marking> markings)
    {
        _markings = markings;
    }

    public IEnumerator GetEnumerator()
    {
        return new MarkingsEnumerator(_markings, false);
    }
}

public sealed class ReverseMarkingEnumerator : IEnumerable
{
    private List<Marking> _markings;

    public ReverseMarkingEnumerator(List<Marking> markings)
    {
        _markings = markings;
    }

    public IEnumerator GetEnumerator()
    {
        return new MarkingsEnumerator(_markings, true);
    }
}

// TODO: Maybe put points logic into here too? It would make some sense
// but it would have to be a template loaded in, otherwise clients could
// send really invalid points sets that would have to be verified on the
// server/client every time
//
// currently marking point constraints are just validated upon data send
// from the client's UI (which might be a nono, means that connections
// can just send garbage marking sets and it will save on the server)
// and when an entity is rendered, (which is OK enough)
//
// equally, we'd need to access references every time we wanted to get
// the managers required, which... not *terrible* if we do null default
// params
[Serializable, NetSerializable]
public class MarkingsSet : IEnumerable, IEquatable<MarkingsSet>
{
    // if you want a rust style VecDeque, you're looking at
    // the wrong place, i just wanted a similar API + some
    // markings specific functions
    private List<Marking> _markings = new();

    public int Count
    {
        get => _markings.Count;
    }

    public MarkingsSet()
    {
    }

    public MarkingsSet(List<Marking> markings)
    {
        _markings = markings;
    }

    public MarkingsSet(MarkingsSet other)
    {
        _markings = new(other._markings);
    }

    public Marking this[int idx] => Index(idx);

    public Marking Index(int idx)
    {
        return _markings[idx];
    }

    // Gets a marking idx spaces from the back of the list.
    public Marking IndexReverse(int idx)
    {
        return _markings[_markings.Count - 1 - idx];
    }

    public void AddFront(Marking marking)
    {
        _markings.Insert(0, marking);
    }

    public void AddBack(Marking marking)
    {
        _markings.Add(marking);
    }

    public bool Remove(Marking marking)
    {
        return _markings.Remove(marking);
    }

    public bool Contains(Marking marking)
    {
        return _markings.Contains(marking);
    }

    public int FindIndexOf(string id)
    {
        return _markings.FindIndex(m => m.MarkingId == id);
    }

    public bool TryGetMarking(string id, [NotNullWhen(true)] out Marking? marking)
    {
        var index = FindIndexOf(id);

        marking = index > 0 ? _markings[index] : null;

        return index > 0;
    }

    // Shifts a marking's rank upwards (i.e., towards the front of the list)
    public void ShiftRankUp(int idx)
    {
        if (idx < 0 || idx >= _markings.Count || idx - 1 < 0)
        {
            return;
        }

        var temp = _markings[idx - 1];
        _markings[idx - 1] = _markings[idx];
        _markings[idx] = temp;
    }

    // Shifts up from the back (i.e., 2nd position from end)
    public void ShiftRankUpFromEnd(int idx)
    {
        ShiftRankUp(Count - idx - 1);
    }

    // Ditto, but the opposite direction.
    public void ShiftRankDown(int idx)
    {
        if (idx < 0 || idx >= _markings.Count || idx + 1 >= _markings.Count)
        {
            return;
        }

        var temp = _markings[idx + 1];
        _markings[idx + 1] = _markings[idx];
        _markings[idx] = temp;
    }

    // Ditto as above.
    public void ShiftRankDownFromEnd(int idx)
    {
        ShiftRankDown(Count - idx - 1);
    }

    // Ensures that all markings in a set are valid.
    public static MarkingsSet EnsureValid(MarkingsSet set, MarkingManager? manager = null)
    {
        IoCManager.Resolve(ref manager);

        for (var i = set._markings.Count - 1; i >= 0; i--)
        {
            var marking = set._markings[i];
            if (manager.IsValidMarking(marking, out var markingProto))
            {
                if (marking.MarkingColors.Count != markingProto.Sprites.Count)
                {
                    set._markings[i] = new Marking(marking.MarkingId, markingProto.Sprites.Count);
                }
            }
            else
            {
                set._markings.RemoveAt(i);
            }
        }

        return set;
    }

    // Filters out markings based on species.
    public static MarkingsSet FilterSpecies(MarkingsSet set, string species, MarkingManager? manager = null)
    {
        IoCManager.Resolve(ref manager);
        var newList = set._markings.Where(marking =>
        {
            if (!manager.Markings().TryGetValue(marking.MarkingId, out MarkingPrototype? prototype))
            {
                return false;
            }

            if (prototype.SpeciesRestrictions != null)
            {
                if (!prototype.SpeciesRestrictions.Contains(species))
                {
                    return false;
                }
            }

            return true;
        }).ToList();

        set._markings = newList;

        return set;
    }

    // Processes a MarkingsSet using the given dictionary of MarkingPoints.
    public static MarkingsSet ProcessPoints(MarkingsSet set, Dictionary<MarkingCategories, MarkingPoints> points, MarkingManager? manager = null)
    {
        IoCManager.Resolve(ref manager);
        var finalSet = new List<Marking>();

        foreach (var marking in set)
        {
            if (manager.Markings().TryGetValue(marking.MarkingId, out MarkingPrototype? markingPrototype))
            {
                if (points.TryGetValue(markingPrototype.MarkingCategory, out var pointsRemaining))
                {
                    if (pointsRemaining.Points == 0)
                    {
                        continue;
                    }

                    pointsRemaining.Points--;

                    finalSet.Add(marking);
                }
                else
                {
                    // points don't exist otherwise
                    finalSet.Add(marking);
                }
            }
        }

        set._markings = finalSet;

        return set;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator) GetEnumerator();
    }

    public MarkingsEnumerator GetEnumerator()
    {
        return new MarkingsEnumerator(_markings, false);
    }

    public IEnumerator GetReverseEnumerator()
    {
        return (IEnumerator) new MarkingsEnumerator(_markings, true);
    }

    public bool Equals(MarkingsSet? set)
    {
        if (set == null)
        {
            return false;
        }

        return _markings.SequenceEqual(set._markings);
    }
}

public class MarkingsEnumerator : IEnumerator
{
    private List<Marking> _markings;
    private bool _reverse;

    int position;

    public MarkingsEnumerator(List<Marking> markings, bool reverse)
    {
        _markings = markings;
        _reverse = reverse;

        if (_reverse)
        {
            position = _markings.Count;
        }
        else
        {
            position = -1;
        }
    }

    public bool MoveNext()
    {
        if (_reverse)
        {
            position--;
            return (position >= 0);
        }
        else
        {
            position++;
            return (position < _markings.Count);
        }
    }

    public void Reset()
    {
        if (_reverse)
        {
            position = _markings.Count;
        }
        else
        {
            position = -1;
        }
    }

    object IEnumerator.Current
    {
        get => _markings[position];
    }

    public Marking Current
    {
        get => _markings[position];
    }
}
