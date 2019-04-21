namespace UQFrameWork.Demo
{
    class EntityViewModel
    {
        public EntityViewModel(Entity entity, bool isNew = false)
        {
            Entity = entity;
            IsNew = isNew;
        }

        public Entity Entity { get; }

        public bool IsNew { get; }

        public bool IsDeleted { set; get; }
    }
}
