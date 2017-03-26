using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceQueryBuffer : QueryBuffer
    {
        private IStateManager _mStateManager => (IStateManager) _stateManagerFieldInfo.GetValue(this);
        private IChangeDetector _mChangeDetector => (IChangeDetector)_changeDetectorFieldInfo.GetValue(this);
        private ConditionalWeakTable<object, object> _mValueBuffers => (ConditionalWeakTable<object, object>)_valueBuffersFieldInfo.GetValue(this);

        private IWeakReferenceIdentityMap _identityMap0;
        private IWeakReferenceIdentityMap _identityMap1;
        private Dictionary<IKey, IWeakReferenceIdentityMap> _identityMaps;

        private readonly FieldInfo _stateManagerFieldInfo;
        private readonly FieldInfo _changeDetectorFieldInfo;
        private readonly FieldInfo _valueBuffersFieldInfo;
        private readonly INavigationExpressionCollection _collection;
        
        public ReplaceQueryBuffer(IStateManager stateManager, IChangeDetector changeDetector, INavigationExpressionCollection collection) : base(stateManager, changeDetector)
        {
            var type = typeof(QueryBuffer);
            _stateManagerFieldInfo = type.GetField("_stateManager", BindingFlags.NonPublic | BindingFlags.Instance);
            _changeDetectorFieldInfo = type.GetField("_changeDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            _valueBuffersFieldInfo = type.GetField("_valueBuffers", BindingFlags.NonPublic | BindingFlags.Instance);

            _collection = collection;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public override void Include(
            QueryContext queryContext,
            object entity,
            IReadOnlyList<INavigation> navigationPath,
            IReadOnlyList<IRelatedEntitiesLoader> relatedEntitiesLoaders,
            bool queryStateManager)
            => Include(
                queryContext,
                entity,
                navigationPath,
                relatedEntitiesLoaders,
                currentNavigationIndex: 0,
                queryStateManager: queryStateManager);

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void Include(
            QueryContext queryContext,
            object entity,
            IReadOnlyList<INavigation> navigationPath,
            IReadOnlyList<IRelatedEntitiesLoader> relatedEntitiesLoaders,
            int currentNavigationIndex,
            bool queryStateManager)
        {
            if (entity == null || currentNavigationIndex == navigationPath.Count)
            {
                return;
            }

            var navigation = navigationPath[currentNavigationIndex];
            var keyComparer = IncludeCore(entity, navigation);
            var key = navigation.GetTargetType().FindPrimaryKey();

            // compile filter
            //var expressions = _collection.TryGet(navigation);
            //var compiled = expressions!= null && expressions.Count > 0 ? ((LambdaExpression) expressions.First()).Compile() : null;
            
            LoadNavigationProperties(
                entity,
                navigationPath,
                currentNavigationIndex,
                relatedEntitiesLoaders[currentNavigationIndex]
                    .Load(queryContext, keyComparer)
                    .Select(eli =>
                    {
                        var targetEntity = GetEntity(key, eli, queryStateManager, throwOnNullKey: false);

                        Include(
                            queryContext,
                            targetEntity,
                            navigationPath,
                            relatedEntitiesLoaders,
                            currentNavigationIndex + 1,
                            queryStateManager);

                        return targetEntity;
                    })
                    .Where(e =>
                    {
                        //if (compiled == null)
                        //{
                        //    return e != null;
                        //}
                        //return e != null && (bool)compiled.DynamicInvoke(e);

                        return e != null;
                    })
                    .ToList(),
                queryStateManager);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private IIncludeKeyComparer IncludeCore(
            object entity,
            INavigation navigation)
        {
            var identityMap = GetOrCreateIdentityMap(navigation.ForeignKey.PrincipalKey);

            object boxedValueBuffer;
            if (!_mValueBuffers.TryGetValue(entity, out boxedValueBuffer))
            {
                var entry = _mStateManager.TryGetEntry(entity);

                Debug.Assert(entry != null);

                return identityMap.CreateIncludeKeyComparer(navigation, entry);
            }

            return identityMap.CreateIncludeKeyComparer(navigation, (ValueBuffer)boxedValueBuffer);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void LoadNavigationProperties(
            object entity,
            IReadOnlyList<INavigation> navigationPath,
            int currentNavigationIndex,
            IReadOnlyList<object> relatedEntities,
            bool tracking)
        {
            _mChangeDetector.Suspend();
            try
            {
                var navigation = navigationPath[currentNavigationIndex];
                var inverseNavigation = navigation.FindInverse();

                if (navigation.IsDependentToPrincipal()
                    && relatedEntities.Any())
                {
                    var relatedEntity = relatedEntities[0];

                    SetNavigation(entity, navigation, relatedEntity, tracking);

                    if (inverseNavigation != null)
                    {
                        if (inverseNavigation.IsCollection())
                        {
                            AddToCollection(relatedEntity, inverseNavigation, entity, tracking);
                        }
                        else
                        {
                            SetNavigation(relatedEntity, inverseNavigation, entity, tracking);
                        }
                    }
                }
                else
                {
                    if (navigation.IsCollection())
                    {
                        AddRangeToCollection(entity, navigation, relatedEntities, tracking);

                        if (inverseNavigation != null)
                        {
                            var setter = inverseNavigation.GetSetter();

                            foreach (var relatedEntity in relatedEntities)
                            {
                                SetNavigation(relatedEntity, inverseNavigation, setter, entity, tracking);
                            }
                        }
                    }
                    else if (relatedEntities.Any())
                    {
                        var relatedEntity = relatedEntities[0];

                        SetNavigation(entity, navigation, relatedEntity, tracking);

                        if (inverseNavigation != null)
                        {
                            SetNavigation(relatedEntity, inverseNavigation, entity, tracking);
                        }
                    }
                }
            }
            finally
            {
                _mChangeDetector.Resume();
            }
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void SetNavigation(object entity, INavigation navigation, object value, bool tracking)
            => SetNavigation(entity, navigation, navigation.GetSetter(), value, tracking);

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void SetNavigation(object entity, INavigation navigation, IClrPropertySetter setter, object value, bool tracking)
        {
            setter.SetClrValue(entity, value);

            if (tracking)
            {
                _mStateManager.TryGetEntry(entity)?.SetRelationshipSnapshotValue(navigation, value);
            }
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void AddToCollection(object entity, INavigation navigation, object value, bool tracking)
        {
            navigation.GetCollectionAccessor().Add(entity, value);

            if (tracking)
            {
                _mStateManager.TryGetEntry(entity)?.AddToCollectionSnapshot(navigation, value);
            }
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private void AddRangeToCollection(object entity, INavigation navigation, IEnumerable<object> values, bool tracking)
        {
            navigation.GetCollectionAccessor().AddRange(entity, values);

            if (tracking)
            {
                var entry = _mStateManager.TryGetEntry(entity);

                // Added navigation.GetPropertyIndexes() != null
                if (entry != null && navigation.GetPropertyIndexes() != null)
                {
                    entry.AddRangeToCollectionSnapshot(navigation, values);
                }
            }
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private IWeakReferenceIdentityMap GetOrCreateIdentityMap(IKey key)
        {
            if (_identityMap0 == null)
            {
                _identityMap0 = key.GetWeakReferenceIdentityMapFactory()();
                return _identityMap0;
            }

            if (_identityMap0.Key == key)
            {
                return _identityMap0;
            }

            if (_identityMap1 == null)
            {
                _identityMap1 = key.GetWeakReferenceIdentityMapFactory()();
                return _identityMap1;
            }

            if (_identityMap1.Key == key)
            {
                return _identityMap1;
            }

            if (_identityMaps == null)
            {
                _identityMaps = new Dictionary<IKey, IWeakReferenceIdentityMap>();
            }

            IWeakReferenceIdentityMap identityMap;
            if (!_identityMaps.TryGetValue(key, out identityMap))
            {
                identityMap = key.GetWeakReferenceIdentityMapFactory()();
                _identityMaps[key] = identityMap;
            }
            return identityMap;
        }
    }
}
