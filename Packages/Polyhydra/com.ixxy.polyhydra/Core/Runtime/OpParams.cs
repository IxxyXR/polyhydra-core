using System;
using System.Collections.Generic;
using System.Linq;

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

    public class Filter
    {
        public static Filter All = new Filter(p => true);
        public static Filter Outer = new Filter(p => p.poly.Faces[p.index].HasNakedEdge());
        public static Filter Existing = new Filter(p => p.poly.FaceRoles[p.index]==PolyMesh.Roles.Existing);
        
        public Func<FilterParams, bool> eval;

        public Filter(Func<FilterParams, bool> func)
        {
            eval = func;
        }
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
        public string tags;

        public float GetValueA(PolyMesh poly, int index) => funcA?.eval.Invoke(new FilterParams(poly, index)) ?? 0;
        public float GetValueB(PolyMesh poly, int index) => funcB?.eval.Invoke(new FilterParams(poly, index)) ?? 0;

        private List<Tuple<string, PolyMesh.TagType>> _tagList;

        public OpParams(
            string selectByTags = "" 
        )
        {
            tags = selectByTags;
        }
        
        public OpParams(
            Filter filter 
        )
        {
            this.filter = filter;
        }
        
        public OpParams(
            float a,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            tags = selectByTags;
        }

        public OpParams(
            float a, float b,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            funcB = new OpFunc(b);
            tags = selectByTags;
        }
        
        public OpParams(
            float a, float b,
            Filter filter,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            funcB = new OpFunc(b);
            this.filter = filter;
            tags = selectByTags;
        }
        
        public OpParams(
            OpFunc a,
            Filter filter,
            string selectByTags = ""
        )
        {
            funcA = a;
            this.filter = filter;
            tags = selectByTags;
        }
        
        public OpParams(
            OpFunc a,
            string selectByTags = "" 
        )
        {
            funcA = a;
            tags = selectByTags;
        }

        public OpParams(
            float a,
            OpFunc b,
            Filter filter,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            funcB = b;
            this.filter = filter;
            tags = selectByTags;
        }
        
        public OpParams(
            float a,
            OpFunc b,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            funcB = b;
            tags = selectByTags;
        }
        
        public OpParams(
            float a,
            Filter filter,
            string selectByTags = "" 
        )
        {
            funcA = new OpFunc(a);
            this.filter = filter;
            tags = selectByTags;
        }
        
        public OpParams(
            Filter filter,
            string selectByTags = "" 
        )
        {
            this.filter = filter;
            tags = selectByTags;
        }

        public List<Tuple<string, PolyMesh.TagType>> TagListFromString(bool introvert=false)
        {
            if (_tagList == null)
            {
                _tagList = TagListFromString(tags);
            }

            return _tagList;
        }

        public static List<Tuple<string, PolyMesh.TagType>> TagListFromString(string tagString, bool introvert=false)
        {
            var tagList = new List<Tuple<string, PolyMesh.TagType>>();
            if (!string.IsNullOrEmpty(tagString))
            {
                var substrings = tagString.Split(',');
                if (substrings.Length == 0) substrings = new[] {tagString};
                var tagType = introvert ? PolyMesh.TagType.Introvert : PolyMesh.TagType.Extrovert;
                tagList = substrings.Select(item => new Tuple<string, PolyMesh.TagType>(item, tagType)).ToList();
            }

            return tagList;
        }
    }
}