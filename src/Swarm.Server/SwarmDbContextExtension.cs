using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Swarm.Basic.Entity;
using Swarm.Server.Models;

namespace Swarm.Server
{
    public static class SwarmDbContextExtension
    {
        public static PaginationQueryOutput PageList<TEntity, TKey, TOrdery>(this DbSet<TEntity> dbSet,
            PaginationQueryInput input,
            Expression<Func<TEntity, bool>> where = null,
            Expression<Func<TEntity, TOrdery>> orderyBy = null) where TEntity : class, IEntity<TKey>
        {
            PaginationQueryOutput output = new PaginationQueryOutput();
            IQueryable<TEntity> entities = dbSet.AsQueryable();
            if (where != null)
            {
                entities = entities.Where(where);
            }

            output.Total = entities.Count();

            if (orderyBy == null)
            {
                if (input.SortByDesc)
                {
                    entities = entities.OrderByDescending(e => e.Id).Skip((input.Page.Value - 1) * input.Size.Value)
                        .Take(input.Size.Value);
                }
                else
                {
                    entities = entities.Skip((input.Page.Value - 1) * input.Size.Value).Take(input.Size.Value);
                }
            }
            else
            {
                if (input.SortByDesc)
                {
                    entities = entities.OrderByDescending(orderyBy).Skip((input.Page.Value - 1) * input.Size.Value)
                        .Take(input.Size.Value);
                }
                else
                {
                    entities = entities.OrderBy(orderyBy).Skip((input.Page.Value - 1) * input.Size.Value)
                        .Take(input.Size.Value);
                }
            }

            output.Page = input.Page.Value;
            output.Size = input.Size.Value;
            output.Result = output.Total == 0 ? new List<TEntity>() : entities.ToList();
            return output;
        }
    }
}