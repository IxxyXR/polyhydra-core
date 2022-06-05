using System;

namespace Polyhydra.Core
{
    
    public struct FilterParams
    {
        public FilterParams(PolyMesh p, int i)
        {
            poly = p;
            index = i;
        }

        public PolyMesh poly;
        public int index;

    }
    
    public class OpFunc
    {
        public Func<FilterParams, float> eval;

        public OpFunc(float a)
        {
            eval = p => a;
        }

        public OpFunc(Func<FilterParams, float> func)
        {
            eval = func;
        }
    }

    public class OpParams
    {
        public OpFunc funcA;
        public OpFunc funcB;
        public Filter filter;

        public float OriginalParamA => _OriginalParamA;
        public float OriginalParamB => _OriginalParamB;
        public string stringParam;
        
        private float _OriginalParamA;
        private float _OriginalParamB;


        public float GetValueA(PolyMesh poly, int index) => funcA?.eval.Invoke(new FilterParams(poly, index)) ?? 0;
        public float GetValueB(PolyMesh poly, int index) => funcB?.eval.Invoke(new FilterParams(poly, index)) ?? 0;
        
        public OpParams(Filter filter=null)
        {
            this.filter = filter;
        }
        
        public OpParams(float a, Filter filter=null)
        {
            _OriginalParamA = a;
            funcA = new OpFunc(a);
            this.filter = filter;
        }
        
        public OpParams(float a, float b, string s=null, Filter filter=null)
        {
            _OriginalParamA = a;
            _OriginalParamB = b;
            funcA = new OpFunc(a);
            funcB = new OpFunc(b);
            stringParam = s;
            this.filter = filter;
        }

        public OpParams(OpFunc a, Filter filter=null)
        {
            funcA = a;
            this.filter = filter;
        }

        public OpParams(float a, OpFunc b, Filter filter=null)
        {
            _OriginalParamA = a;
            funcA = new OpFunc(a);
            funcB = b;
            this.filter = filter;
        }

        public OpParams(OpFunc a, OpFunc b, string s=null, Filter filter=null)
        {
            funcA = a;
            funcB = b;
            stringParam = s;
            this.filter = filter;
        }
    }
}