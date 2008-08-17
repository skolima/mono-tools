// Author:
// Massimiliano Mantione (massi@ximian.com)
//
// (C) 2008 Novell, Inc  http://www.novell.com
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;	
using System.IO;
using System.Collections.Generic;

namespace Mono.Profiler {
	public interface ILoadedElement {
		uint ID {get;}
		string Name {get;}
	}
	public interface ILoadedClass : ILoadedElement {
		uint Size {get;}
	}
	public interface ILoadedMethod<LC> : ILoadedElement where LC : ILoadedClass {
		LC Class {get;}
	}
	public interface IUnmanagedFunctionFromID<MR,UFR> : ILoadedElement where UFR : IUnmanagedFunctionFromRegion where MR : IExecutableMemoryRegion<UFR> {
		MR Region {get;}
	}
	public interface IUnmanagedFunctionFromRegion {
		string Name {get; set;}
		uint StartOffset {get; set;}
		uint EndOffset {get; set;}
	}
	public interface IExecutableMemoryRegion<UFR> : ILoadedElement where UFR : IUnmanagedFunctionFromRegion {
		ulong StartAddress {get;}
		ulong EndAddress {get;}
		uint FileOffset {get;}
		UFR NewFunction (string name, uint offset);
		UFR GetFunction (uint offset);
		UFR[] Functions {get;}
		void SortFunctions ();
	}
	public interface IHeapObject<HO,LC> where HO: IHeapObject<HO,LC> where LC : ILoadedClass {
		ulong ID {get;}
		LC Class {get;}
		uint Size {get;}
		HO[] References {get;}
		HO[] BackReferences {get;}
	}
	public interface IHeapSnapshot<HO,LC> where HO: IHeapObject<HO,LC> where LC : ILoadedClass {
		uint Collection {get;}
		ulong StartCounter {get;}
		DateTime StartTime {get;}
		ulong EndCounter {get;}
		DateTime EndTime {get;}
		HO NewHeapObject (ulong id, LC c, uint size, ulong[] referenceIds, int referencesCount);
		HO GetHeapObject (ulong id);
		HO[] HeapObjects {get;}
		bool RecordSnapshot {get;}
	}
	
	public interface ILoadedElementFactory<LC,LM,UFR,UFI,MR,HO,HS> where LC : ILoadedClass where LM : ILoadedMethod<LC> where UFR : IUnmanagedFunctionFromRegion where UFI : IUnmanagedFunctionFromID<MR,UFR> where MR : IExecutableMemoryRegion<UFR> where HO: IHeapObject<HO,LC> where HS: IHeapSnapshot<HO,LC> {
		LC NewClass (uint id, string name, uint size);
		LM NewMethod (uint id, LC c, string name);
		MR NewExecutableMemoryRegion (uint id, string fileName, uint fileOffset, ulong startAddress, ulong endAddress);
		UFI NewUnmanagedFunction (uint id, string name, MR region);
		HS NewHeapSnapshot (uint collection, ulong startCounter, DateTime startTime, ulong endCounter, DateTime endTime, LC[] initialAllocations, bool recordSnapshot);
		bool RecordHeapSnapshots {get; set;}
	}
	
	public interface ILoadedElementHandler<LC,LM,UFR,UFI,MR,HO,HS> : ILoadedElementFactory<LC,LM,UFR,UFI,MR,HO,HS> where LC : ILoadedClass where LM : ILoadedMethod<LC> where UFR : IUnmanagedFunctionFromRegion where UFI : IUnmanagedFunctionFromID<MR,UFR> where MR : IExecutableMemoryRegion<UFR> where HO: IHeapObject<HO,LC> where HS: IHeapSnapshot<HO,LC> {
		LC[] Classes {get;}
		LC GetClass (uint id);
		LM[] Methods {get;}
		LM GetMethod (uint id);
		MR[] ExecutableMemoryRegions {get;}
		MR GetExecutableMemoryRegion (uint id);
		MR GetExecutableMemoryRegion (ulong address);
		void InvalidateExecutableMemoryRegion (uint id);
		void SortExecutableMemoryRegions ();
		UFI[] UnmanagedFunctionsByID {get;}
		UFI GetUnmanagedFunctionByID (uint id);
		HS[] HeapSnapshots {get;}
	}
	public interface IProfilerEventHandler<LC,LM,UFR,UFI,MR,EH,HO,HS> where LC : ILoadedClass where LM : ILoadedMethod<LC> where UFR : IUnmanagedFunctionFromRegion where UFI : IUnmanagedFunctionFromID<MR,UFR> where MR : IExecutableMemoryRegion<UFR> where EH : ILoadedElementHandler<LC,LM,UFR,UFI,MR,HO,HS> where HO: IHeapObject<HO,LC> where HS: IHeapSnapshot<HO,LC> {
		EH LoadedElements {get;}
		
		void Start (uint version, string runtimeFile, ProfilerFlags flags, ulong startCounter, DateTime startTime);
		void End (uint version, ulong endCounter, DateTime endTime);
		
		void StartBlock (ulong startCounter, DateTime startTime, ulong threadId);
		void EndBlock (ulong endCounter, DateTime endTime, ulong threadId);
		
		void ModuleLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success);
		void ModuleUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name);
		void AssemblyLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success);
		void AssemblyUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name);
		void ApplicationDomainLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success);
		void ApplicationDomainUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name);
		
		void SetCurrentThread (ulong threadId);
		
		void ClassStartLoad (LC c, ulong counter);
		void ClassEndLoad (LC c, ulong counter, bool success);
		void ClassStartUnload (LC c, ulong counter);
		void ClassEndUnload (LC c, ulong counter);
		
		void Allocation (LC c, uint size);
		void Exception (LC c, ulong counter);
		
		void MethodEnter (LM m, ulong counter);
		void MethodExit (LM m, ulong counter);
		void MethodJitStart (LM m, ulong counter);
		void MethodJitEnd (LM m, ulong counter, bool success);
		void MethodFreed (LM m, ulong counter);
		
		void MethodStatisticalHit (LM m);
		void UnknownMethodStatisticalHit ();
		void UnmanagedFunctionStatisticalHit (UFR f);
		void UnmanagedFunctionStatisticalHit (UFI f);
		void UnknownUnmanagedFunctionStatisticalHit (MR region, uint offset);
		void UnknownUnmanagedFunctionStatisticalHit (ulong address);
		void StatisticalCallChainStart (uint chainDepth);
		
		void ThreadStart (ulong threadId, ulong counter);
		void ThreadEnd (ulong threadId, ulong counter);
		
		void GarbageCollectionStart (uint collection, uint generation, ulong counter);
		void GarbageCollectionEnd (uint collection, uint generation, ulong counter);
		void GarbageCollectionMarkStart (uint collection, uint generation, ulong counter);
		void GarbageCollectionMarkEnd (uint collection, uint generation, ulong counter);
		void GarbageCollectionSweepStart (uint collection, uint generation, ulong counter);
		void GarbageCollectionSweepEnd (uint collection, uint generation, ulong counter);
		void GarbageCollectionResize (uint collection, ulong newSize);
		void GarbageCollectionStopWorldStart (uint collection, uint generation, ulong counter);
		void GarbageCollectionStopWorldEnd (uint collection, uint generation, ulong counter);
		void GarbageCollectionStartWorldStart (uint collection, uint generation, ulong counter);
		void GarbageCollectionStartWorldEnd (uint collection, uint generation, ulong counter);
		
		void HeapReportStart (HS snapshot);
		void HeapObjectUnreachable (LC c, uint size);
		void HeapObjectReachable (HO o);
		void HeapReportEnd (HS snapshot);
		
		void AllocationSummaryStart (uint collection, ulong startCounter, DateTime startTime);
		void ClassAllocationSummary (LC c, uint reachableInstances, uint reachableBytes, uint unreachableInstances, uint unreachableBytes);
		void AllocationSummaryEnd (uint collection, ulong endCounter, DateTime endTime);
	}
	
	public class BaseLoadedElement : ILoadedElement {
		uint id;
		public uint ID {
			get {
				return id;
			}
		}
		
		string name;
		public string Name {
			get {
				return name;
			}
		}
		
		public BaseLoadedElement (uint id, string name) {
			this.id = id;
			this.name = name;
		}
	}
	public class BaseLoadedClass : BaseLoadedElement, ILoadedClass {
		uint size;
		public uint Size {
			get {
				return size;
			}
		}
		
		public BaseLoadedClass (uint id, string name, uint size) : base (id, name) {
			this.size = size;
		}
	}
	public class BaseLoadedMethod<LC> : BaseLoadedElement, ILoadedMethod<LC> where LC : ILoadedClass {
		LC c;
		public LC Class {
			get {
				return c;
			}
		}
		
		public BaseLoadedMethod (uint id, LC c, string name) : base (id, name) {
			this.c = c;
		}
	}
	public class BaseUnmanagedFunctionFromRegion : IUnmanagedFunctionFromRegion {
		IExecutableMemoryRegion<IUnmanagedFunctionFromRegion> region;
		public IExecutableMemoryRegion<IUnmanagedFunctionFromRegion> Region {
			get {
				return region;
			}
			set {
				region = value;
			}
		}
		string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		uint startOffset;
		public uint StartOffset {
			get {
				return startOffset;
			}
			set {
				startOffset = value;
			}
		}
		uint endOffset;
		public uint EndOffset {
			get {
				return endOffset;
			}
			set {
				endOffset = value;
			}
		}
		public BaseUnmanagedFunctionFromRegion () {
			this.region = null;
			this.name = null;
			this.startOffset = 0;
			this.endOffset = 0;
		}
		public BaseUnmanagedFunctionFromRegion (IExecutableMemoryRegion<IUnmanagedFunctionFromRegion> region, string name, uint startOffset, uint endOffset) {
			this.region = region;
			this.name = name;
			this.startOffset = startOffset;
			this.endOffset = endOffset;
		}
	}
	public class BaseUnmanagedFunctionFromID<MR,UFR>: BaseLoadedElement, IUnmanagedFunctionFromID<MR,UFR>  where UFR : IUnmanagedFunctionFromRegion where MR : IExecutableMemoryRegion<UFR> {
		MR region;
		public MR Region {
			get {
				return region;
			}
		}
		
		public BaseUnmanagedFunctionFromID (uint id, string name, MR region) : base (id, name) {
			this.region = region;
		}
	}
	
	public class HeapObject<LC> : IHeapObject<HeapObject<LC>,LC> where LC : ILoadedClass {
		ulong id;
		public ulong ID {
			get {
				return id;
			}
		}
		LC c;
		public LC Class {
			get {
				return c;
			}
			internal set {
				c = value;
			}
		}
		
		uint size;
		public uint Size {
			get {
				return size;
			}
			internal set {
				size = value;
			}
		}
		
		static HeapObject<LC>[] emptyReferences = new HeapObject<LC> [0];
		public static HeapObject<LC>[] EmptyReferences {
			get {
				return emptyReferences;
			}
		}
		
		HeapObject<LC>[] references;
		public HeapObject<LC>[] References {
			get {
				return references;
			}
			internal set {
				references = value;
			}
		}
		
		HeapObject<LC>[] backReferences;
		public HeapObject<LC>[] BackReferences {
			get {
				return backReferences;
			}
			internal set {
				backReferences = value;
			}
		}
		
		int backReferencesCounter;
		internal void IncrementBackReferences () {
			backReferencesCounter ++;
		}
		internal void AllocateBackReferences () {
			if (references != null) {
				int referencesCount = 0;
				foreach (HeapObject<LC> reference in references) {
					if (reference != null) {
						referencesCount ++;
					}
				}
				if (referencesCount != references.Length) {
					if (referencesCount > 0) {
						HeapObject<LC>[] newReferences = new HeapObject<LC> [referencesCount];
						referencesCount = 0;
						foreach (HeapObject<LC> reference in references) {
							if (reference != null) {
								newReferences [referencesCount] = reference;
								referencesCount ++;
							}
						}
						references = newReferences;
					} else {
						references = emptyReferences;
					} 
				}
			} else {
				references = emptyReferences;
			}
			
			if (backReferencesCounter > 0) {
				backReferences = new HeapObject<LC> [backReferencesCounter];
				backReferencesCounter = 0;
			} else {
				references = emptyReferences;
			}
		}
		internal void AddBackReference (HeapObject<LC> heapObject) {
			backReferences [backReferencesCounter] = heapObject;
			backReferencesCounter ++;
		}
		
		public HeapObject (ulong id, LC c, uint size, HeapObject<LC>[] references) {
			this.id = id;
			this.c = c;
			this.size = size;
			this.references = references;
			this.backReferences = null;
			this.backReferencesCounter = 0;
		}
		public HeapObject (ulong id) {
			this.id = id;
			this.c = default(LC);
			this.size = 0;
			this.references = null;
			this.backReferences = null;
			this.backReferencesCounter = 0;
		}
	}
	
	public abstract class BaseHeapSnapshot<HO,LC> : IHeapSnapshot<HeapObject<LC>,LC> where LC : ILoadedClass {
		Dictionary<ulong,HeapObject<LC>> heap;
		bool backReferencesInitialized;
		
		uint collection;
		public uint Collection {
			get {
				return collection;
			}
		}
		
		ulong startCounter;
		public ulong StartCounter {
			get {
				return startCounter;
			}
		}
		DateTime startTime;
		public DateTime StartTime {
			get {
				return startTime;
			}
		}
		ulong endCounter;
		public ulong EndCounter {
			get {
				return endCounter;
			}
		}
		DateTime endTime;
		public DateTime EndTime {
			get {
				return endTime;
			}
		}
		
		public HeapObject<LC> NewHeapObject (ulong id, LC c, uint size, ulong[] referenceIds, int referencesCount) {
			if (backReferencesInitialized) {
				throw new Exception ("Cannot create heap objects after backReferencesInitialized is true");
			}
			
			if (recordSnapshot) {
				HeapObject<LC>[] references = new HeapObject<LC>[referencesCount];
				HeapObject<LC> result = GetOrCreateHeapObject (id);
				for (int i = 0; i < references.Length; i++) {
					references [i] = GetOrCreateHeapObject (referenceIds [i]);
					references [i].IncrementBackReferences ();
				}
				result.References = references;
				result.Size = size;
				result.Class = c;
				return result;
			} else {
				return null;
			}
		}
		
		public void InitializeBackReferences () {
			if (backReferencesInitialized) {
				throw new Exception ("Cannot call InitializeBackReferences twice");
			}
			
			//FIXME: Bad objects should not happen anymore...
			Dictionary<ulong,HeapObject<LC>> badObjects = new Dictionary<ulong,HeapObject<LC>> ();
			
			foreach (HeapObject<LC> heapObject in heap.Values) {
				if (heapObject.Class != null) {
					heapObject.AllocateBackReferences ();
				} else {
					badObjects.Add (heapObject.ID, heapObject);
				}
			}
			
			foreach (ulong id in badObjects.Keys) {
				heap.Remove (id);
			}
			
			foreach (HeapObject<LC> heapObject in heap.Values) {
				foreach (HeapObject<LC> reference in heapObject.References) {
					reference.AddBackReference (heapObject);
				}
			}
			
			backReferencesInitialized = true;
		}
		
		HeapObject<LC> GetOrCreateHeapObject (ulong id) {
			if (recordSnapshot) {
				if (heap.ContainsKey (id)) {
					return heap [id];
				} else {
					HeapObject<LC> result = new HeapObject<LC> (id);
					heap [id] = result;
					return result;
				}
			} else {
				return null;
			}
		}
		
		public HeapObject<LC> GetHeapObject (ulong id) {
			return heap [id];
		}
		
		public HeapObject<LC>[] HeapObjects {
			get {
				HeapObject<LC>[] result = new HeapObject<LC> [heap.Values.Count];
				heap.Values.CopyTo (result, 0);
				return result;
			}
		}
		
		bool recordSnapshot;
		public bool RecordSnapshot {
			get {
				return recordSnapshot;
			}
		}
		
		public BaseHeapSnapshot (uint collection, ulong startCounter, DateTime startTime, ulong endCounter, DateTime endTime, bool recordSnapshot) {
			this.collection = collection;
			this.startCounter = startCounter;
			this.startTime = startTime;
			this.endCounter = endCounter;
			this.endTime = endTime;
			this.recordSnapshot = recordSnapshot;
			heap = new Dictionary<ulong,HeapObject<LC>> ();
			backReferencesInitialized = false;
		}
	}
	
	public struct AllocationClassData<LC> where LC : ILoadedClass  {
		LC c;
		public LC Class {
			get {
				return c;
			}
		}
		uint reachableInstances;
		public uint ReachableInstances {
			get {
				return reachableInstances;
			}
		}
		uint reachableBytes;
		public uint ReachableBytes {
			get {
				return reachableBytes;
			}
		}
		uint unreachableInstances;
		public uint UnreachableInstances {
			get {
				return unreachableInstances;
			}
		}
		uint unreachableBytes;
		public uint UnreachableBytes {
			get {
				return unreachableBytes;
			}
		}
		
		public static Comparison<AllocationClassData<LC>> CompareByReachableBytes = delegate (AllocationClassData<LC> a, AllocationClassData<LC> b) {
			return a.ReachableBytes.CompareTo (b.ReachableBytes);
		};
		public static Comparison<AllocationClassData<LC>> CompareByReachableInstances = delegate (AllocationClassData<LC> a, AllocationClassData<LC> b) {
			return a.ReachableInstances.CompareTo (b.ReachableInstances);
		};
		
		public AllocationClassData (LC c, uint reachableInstances, uint reachableBytes, uint unreachableInstances, uint unreachableBytes) {
			this.c = c;
			this.reachableInstances = reachableInstances;
			this.reachableBytes = reachableBytes;
			this.unreachableInstances = unreachableInstances;
			this.unreachableBytes = unreachableBytes;
		}
	}
	
	public class BaseAllocationSummary<LC> where LC : ILoadedClass {
		uint collection;
		public uint Collection {
			get {
				return collection;
			}
		}
		List<AllocationClassData<LC>> data;
		public AllocationClassData<LC>[] Data {
			get {
				AllocationClassData<LC>[] result = data.ToArray ();
				Array.Sort (result, AllocationClassData<LC>.CompareByReachableBytes);
				Array.Reverse (result);
				return result;
			}
		}
		
		internal void RecordData (LC c, uint reachableInstances, uint reachableBytes, uint unreachableInstances, uint unreachableBytes) {
			data.Add (new AllocationClassData<LC> (c, reachableInstances, reachableBytes, unreachableInstances, unreachableBytes));
		}
		
		ulong startCounter;
		public ulong StartCounter {
			get {
				return startCounter;
			}
		}
		DateTime startTime;
		public DateTime StartTime {
			get {
				return startTime;
			}
		}
		ulong endCounter;
		public ulong EndCounter {
			get {
				return endCounter;
			}
			internal set {
				endCounter = value;
			}
		}
		DateTime endTime;
		public DateTime EndTime {
			get {
				return endTime;
			}
			internal set {
				endTime = value;
			}
		}
		
		public BaseAllocationSummary (uint collection, ulong startCounter, DateTime startTime) {
			this.collection = collection;
			this.startCounter = startCounter;
			this.startTime = startTime;
			this.endCounter = startCounter;
			this.endTime = startTime;
			data = new List<AllocationClassData<LC>> ();
		}
	}
	
	public class BaseProfilerEventHandler<LC,LM,UFR,UFI,MR,EH,HO,HS> : IProfilerEventHandler<LC,LM,UFR,UFI,MR,EH,HO,HS> where LC : ILoadedClass where LM : ILoadedMethod<LC> where UFR : IUnmanagedFunctionFromRegion where UFI : IUnmanagedFunctionFromID<MR,UFR> where MR : IExecutableMemoryRegion<UFR> where EH : ILoadedElementHandler<LC,LM,UFR,UFI,MR,HO,HS> where HO: IHeapObject<HO,LC> where HS: IHeapSnapshot<HO,LC> {
		EH loadedElements;
		public EH LoadedElements {
			get {
				return loadedElements;
			}
		}
		
		public BaseProfilerEventHandler (EH loadedElements) {
			this.loadedElements = loadedElements;
		}
		
		public virtual void Start (uint version, string runtimeFile, ProfilerFlags flags, ulong startCounter, DateTime startTime) {}
		public virtual void End (uint version, ulong endCounter, DateTime endTime) {}
		
		public virtual void StartBlock (ulong startCounter, DateTime startTime, ulong threadId) {}
		public virtual void EndBlock (ulong endCounter, DateTime endTime, ulong threadId) {}
		
		public virtual void ModuleLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success) {}
		public virtual void ModuleUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name) {}
		public virtual void AssemblyLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success) {}
		public virtual void AssemblyUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name) {}
		public virtual void ApplicationDomainLoaded (ulong threadId, ulong startCounter, ulong endCounter, string name, bool success) {}
		public virtual void ApplicationDomainUnloaded (ulong threadId, ulong startCounter, ulong endCounter, string name) {}
		
		public virtual void SetCurrentThread (ulong threadId) {}
		
		public virtual void ClassStartLoad (LC c, ulong counter) {}
		public virtual void ClassEndLoad (LC c, ulong counter, bool success) {}
		public virtual void ClassStartUnload (LC c, ulong counter) {}
		public virtual void ClassEndUnload (LC c, ulong counter) {}
		
		public virtual void Allocation (LC c, uint size) {}
		public virtual void Exception (LC c, ulong counter) {}
		
		public virtual void MethodEnter (LM m, ulong counter) {}
		public virtual void MethodExit (LM m, ulong counter) {}
		public virtual void MethodJitStart (LM m, ulong counter) {}
		public virtual void MethodJitEnd (LM m, ulong counter, bool success) {}
		public virtual void MethodFreed (LM m, ulong counter) {}
		
		public virtual void MethodStatisticalHit (LM m) {}
		public virtual void UnknownMethodStatisticalHit () {}
		public virtual void UnmanagedFunctionStatisticalHit (UFR f) {}
		public virtual void UnmanagedFunctionStatisticalHit (UFI f) {}
		public virtual void UnknownUnmanagedFunctionStatisticalHit (MR region, uint offset) {}
		public virtual void UnknownUnmanagedFunctionStatisticalHit (ulong address) {}
		public virtual void StatisticalCallChainStart (uint chainDepth) {}
		
		public virtual void ThreadStart (ulong threadId, ulong counter) {}
		public virtual void ThreadEnd (ulong threadId, ulong counter) {}
		
		public virtual void GarbageCollectionStart (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionEnd (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionMarkStart (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionMarkEnd (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionSweepStart (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionSweepEnd (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionResize (uint collection, ulong newSize) {}
		public virtual void GarbageCollectionStopWorldStart (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionStopWorldEnd (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionStartWorldStart (uint collection, uint generation, ulong counter) {}
		public virtual void GarbageCollectionStartWorldEnd (uint collection, uint generation, ulong counter) {}
		
		public virtual void HeapReportStart (HS snapshot) {}
		public virtual void HeapObjectUnreachable (LC c, uint size) {}
		public virtual void HeapObjectReachable (HO o) {}
		public virtual void HeapReportEnd (HS snapshot) {}
		
		public virtual void AllocationSummaryStart (uint collection, ulong startCounter, DateTime startTime) {}
		public virtual void ClassAllocationSummary (LC c, uint reachableInstances, uint reachableBytes, uint unreachableInstances, uint unreachableBytes) {}
		public virtual void AllocationSummaryEnd (uint collection, ulong endCounter, DateTime endTime) {}
	}
	
	public class BaseExecutableMemoryRegion<UFR> : BaseLoadedElement, IExecutableMemoryRegion<UFR> where UFR : IUnmanagedFunctionFromRegion, new() {
		uint fileOffset;
		public uint FileOffset {
			get {
				return fileOffset;
			}
		}
		
		ulong startAddress;
		public ulong StartAddress {
			get {
				return startAddress;
			}
		}
		
		ulong endAddress;
		public ulong EndAddress {
			get {
				return endAddress;
			}
		}
		
		List<UFR> functions;
		
		public UFR NewFunction (string name, uint offset) {
			UFR result = new UFR ();
			result.Name = name;
			result.StartOffset = offset;
			result.EndOffset = offset;
			functions.Add (result);
			return result;
		}
		
		public UFR GetFunction (uint offset) {
			int lowIndex = 0;
			int highIndex = functions.Count;
			int middleIndex = lowIndex + ((highIndex - lowIndex) / 2);
			UFR middleFunction = (middleIndex < functions.Count) ? functions [middleIndex] : default (UFR);
			
			while (lowIndex != highIndex) {
				if (middleFunction.StartOffset > offset) {
					if (middleIndex > 0) {
						highIndex = middleIndex;
					} else {
						return default (UFR);
					}
				} else if (middleFunction.EndOffset < offset) {
					if (middleIndex < functions.Count - 1) {
						lowIndex = middleIndex;
					} else {
						return default (UFR);
					}
				} else {
					return middleFunction;
				}
				
				middleIndex = lowIndex + ((highIndex - lowIndex) / 2);
				middleFunction = functions [middleIndex];
			}
			
			if ((middleFunction == null) || (middleFunction.StartOffset > offset) || (middleFunction.EndOffset < offset)) {
				return default (UFR);
			} else {
				return middleFunction;
			}
		}
		
		public UFR[] Functions {
			get {
				UFR[] result = new UFR [functions.Count];
				functions.CopyTo (result);
				return result;
			}
		}
		
		public static Comparison<UFR> CompareByStartOffset = delegate (UFR a, UFR b) {
			return a.StartOffset.CompareTo (b.StartOffset);
		};
		public void SortFunctions () {
			functions.Sort (CompareByStartOffset);
			if (functions.Count > 0) {
				UFR previousFunction = functions [0];
				for (int i = 1; i < functions.Count; i++) {
					UFR currentFunction = functions [i];
					previousFunction.EndOffset = currentFunction.StartOffset - 1;
					previousFunction = currentFunction;
				}
				previousFunction.EndOffset = (uint) (EndAddress - StartAddress);
			}
		}
		
		public BaseExecutableMemoryRegion (uint id, string name, uint fileOffset, ulong startAddress, ulong endAddress) : base (id, name) {
			this.fileOffset = fileOffset;
			this.startAddress = startAddress;
			this.endAddress = endAddress;
			functions = new List<UFR> ();
			
			NativeLibraryReader.FillFunctions<BaseExecutableMemoryRegion<UFR>,UFR> (this);
		}
	}
	
	public class LoadedElementHandler<LC,LM,UFR,UFI,MR,HO,HS> : ILoadedElementHandler<LC,LM,UFR,UFI,MR,HO,HS> where LC : ILoadedClass where LM : ILoadedMethod<LC> where UFR : IUnmanagedFunctionFromRegion where UFI : IUnmanagedFunctionFromID<MR,UFR> where MR : IExecutableMemoryRegion<UFR> where HO: IHeapObject<HO,LC> where HS: IHeapSnapshot<HO,LC> {
		ILoadedElementFactory<LC,LM,UFR,UFI,MR,HO,HS> factory;
		
		int loadedClassesCount;
		LC[] loadedClasses;
		public LC[] Classes {
			get {
				LC[] result = new LC [loadedClassesCount];
				int resultIndex = 0;
				for (int i = 0; i < loadedClasses.Length; i++) {
					LC c = loadedClasses [i];
					if (c != null) {
						result [resultIndex] = c;
						resultIndex ++;
					}
				}
				return result;
			}
		}
		public LC GetClass (uint id) {
			return loadedClasses [(int) id];
		}
		
		int loadedMethodsCount;
		LM[] loadedMethods;
		public LM[] Methods {
			get {
				LM[] result = new LM [loadedMethodsCount];
				int resultIndex = 0;
				for (int i = 0; i < loadedMethods.Length; i++) {
					LM m = loadedMethods [i];
					if (m != null) {
						result [resultIndex] = m;
						resultIndex ++;
					}
				}
				return result;
			}
		}
		public LM GetMethod (uint id) {
			return loadedMethods [(int) id];
		}
		
		Dictionary<uint,MR> memoryRegions;
		List<MR> sortedMemoryRegions;
		public MR[] ExecutableMemoryRegions {
			get {
				MR[] result = new MR [memoryRegions.Count];
				memoryRegions.Values.CopyTo (result, 0);
				return result;
			}
		}
		public MR GetExecutableMemoryRegion (uint id) {
			return memoryRegions [id];
		}
		public MR GetExecutableMemoryRegion (ulong address) {
			int lowIndex = 0;
			int highIndex = sortedMemoryRegions.Count;
			int middleIndex = lowIndex + ((highIndex - lowIndex) / 2);
			MR middleRegion = (middleIndex < sortedMemoryRegions.Count) ? sortedMemoryRegions [middleIndex] : default (MR);
			
			while (lowIndex != highIndex) {
				if (middleRegion.StartAddress > address) {
					if (middleIndex > 0) {
						highIndex = middleIndex;
					} else {
						return default (MR);
					}
				} else if (middleRegion.EndAddress < address) {
					if (middleIndex < sortedMemoryRegions.Count - 1) {
						lowIndex = middleIndex;
					} else {
						return default (MR);
					}
				} else {
					return middleRegion;
				}
				
				middleIndex = lowIndex + ((highIndex - lowIndex) / 2);
				middleRegion = sortedMemoryRegions [middleIndex];
			}
			
			if ((middleRegion == null) || (middleRegion.StartAddress > address) || (middleRegion.EndAddress < address)) {
				return default (MR);
			} else {
				return middleRegion;
			}
		}
		public void InvalidateExecutableMemoryRegion (uint id) {
			MR region = GetExecutableMemoryRegion (id);
			if (region != null) {
				sortedMemoryRegions.Remove (region);
			}
		}
		static Comparison<MR> CompareByStartAddress = delegate (MR a, MR b) {
			return a.StartAddress.CompareTo (b.StartAddress);
		};
		public void SortExecutableMemoryRegions () {
				sortedMemoryRegions.Sort (CompareByStartAddress);
		}
		
		public LC NewClass (uint id, string name, uint size) {
			LC result = factory.NewClass (id, name, size);
			if (loadedClasses.Length <= id) {
				LC[] newLoadedClasses = new LC [((int) id + 1) * 2];
				loadedClasses.CopyTo (newLoadedClasses, 0);
				loadedClasses = newLoadedClasses;
			}
			loadedClasses [(int) id] = result;
			loadedClassesCount ++;
			return result;
		}
		
		public LM NewMethod (uint id, LC c, string name) {
			LM result = factory.NewMethod (id, c, name);
			if (loadedMethods.Length <= id) {
				LM[] newLoadedMethods = new LM [((int) id + 1) * 2];
				loadedMethods.CopyTo (newLoadedMethods, 0);
				loadedMethods = newLoadedMethods;
			}
			loadedMethods [(int) id] = result;
			loadedMethodsCount ++;
			return result;
		}
		
		public MR NewExecutableMemoryRegion (uint id, string fileName, uint fileOffset, ulong startAddress, ulong endAddress) {
			MR result = factory.NewExecutableMemoryRegion (id, fileName, fileOffset, startAddress, endAddress);
			memoryRegions.Add (id, result);
			sortedMemoryRegions.Add (result);
			return result;
		}
		
		List<HS> heapSnapshots;
		public HS NewHeapSnapshot (uint collection, ulong startCounter, DateTime startTime, ulong endCounter, DateTime endTime, LC[] initialAllocations, bool recordSnapshot) {
			HS result = factory.NewHeapSnapshot (collection, startCounter, startTime, endCounter, endTime, initialAllocations, recordSnapshot);
			heapSnapshots.Add (result);
			return result;
		}
		public HS[] HeapSnapshots {
			get {
				HS[] result = new HS [heapSnapshots.Count];
				heapSnapshots.CopyTo (result);
				return result;
			}
		}
		
		int unmanagedFunctionsByIDCount;
		UFI[] unmanagedFunctionsByID;
		public UFI[] UnmanagedFunctionsByID {
			get {
				UFI[] result = new UFI [unmanagedFunctionsByIDCount];
				int resultIndex = 0;
				for (int i = 0; i < unmanagedFunctionsByID.Length; i++) {
					UFI f = unmanagedFunctionsByID [i];
					if (f != null) {
						result [resultIndex] = f;
						resultIndex ++;
					}
				}
				return result;
			}
		}
		public UFI GetUnmanagedFunctionByID (uint id) {
			return unmanagedFunctionsByID [(int) id];
		}
		public UFI NewUnmanagedFunction (uint id, string name, MR region) {
			UFI result = factory.NewUnmanagedFunction (id, name, region);
			if (unmanagedFunctionsByID.Length <= id) {
				UFI[] newUnmanagedFunctionsByID = new UFI [((int) id + 1) * 2];
				unmanagedFunctionsByID.CopyTo (newUnmanagedFunctionsByID, 0);
				unmanagedFunctionsByID = newUnmanagedFunctionsByID;
			}
			unmanagedFunctionsByID [(int) id] = result;
			unmanagedFunctionsByIDCount ++;
			return result;
		}
		
		public bool RecordHeapSnapshots {
			get {
				return factory.RecordHeapSnapshots;
			}
			set {
				factory.RecordHeapSnapshots = value;
			}
		}
		
		public LoadedElementHandler (ILoadedElementFactory<LC,LM,UFR,UFI,MR,HO,HS> factory) {
			this.factory = factory;
			loadedClasses = new LC [1000];
			loadedClassesCount = 0;
			loadedMethods = new LM [5000];
			loadedMethodsCount = 0;
			memoryRegions = new Dictionary<uint,MR> ();
			sortedMemoryRegions = new List<MR> ();
			heapSnapshots = new List<HS> ();
			unmanagedFunctionsByID = new UFI [1000];
			unmanagedFunctionsByIDCount = 0;
		}
	}
}