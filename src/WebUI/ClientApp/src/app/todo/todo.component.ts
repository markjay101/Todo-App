import { Component, TemplateRef, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import {
  TodoListsClient,
  TodoItemsClient,
  TagsClient,
  TodoListDto,
  TodoItemDto,
  TagDto,
  PriorityLevelDto,
  CreateTodoListCommand,
  UpdateTodoListCommand,
  CreateTodoItemCommand,
  UpdateTodoItemDetailCommand,
  CreateTagCommand,
  UpdateTagCommand,
  AssignTagsToTodoItemCommand,
} from '../web-api-client';

@Component({
  selector: 'app-todo-component',
  templateUrl: './todo.component.html',
  styleUrls: ['./todo.component.scss'],
})
export class TodoComponent implements OnInit {
  debug = false;
  deleting = false;
  deleteCountDown = 0;
  deleteCountDownInterval: any;
  lists: TodoListDto[];
  priorityLevels: PriorityLevelDto[];
  selectedList: TodoListDto;
  selectedItem: TodoItemDto;
  newListEditor: any = {};
  listOptionsEditor: any = {};
  newListModalRef: BsModalRef;
  listOptionsModalRef: BsModalRef;
  deleteListModalRef: BsModalRef;
  itemDetailsModalRef: BsModalRef;
  itemDetailsFormGroup = this.fb.group({
    id: [null],
    listId: [null],
    priority: [''],
    note: [''],
    backgroundColour: [''],
  });

  // Supported colors matching the Colour value object
  supportedColors = [
    { name: 'White', value: '#FFFFFF' },
    { name: 'Red', value: '#FF5733' },
    { name: 'Orange', value: '#FFC300' },
    { name: 'Yellow', value: '#FFFF66' },
    { name: 'Green', value: '#CCFF99' },
    { name: 'Blue', value: '#6666FF' },
    { name: 'Purple', value: '#9966CC' },
    { name: 'Grey', value: '#999999' },
  ];

  // Tag-related properties
  tags: TagDto[] = [];
  filteredTags: TagDto[] = [];
  tagSearchText: string = '';
  newTagName: string = '';
  isCreatingTag: boolean = false;
  selectedTag: TagDto | null = null;
  updatingTagId: number | null = null;

  // Tag filtering properties
  selectedFilterTags: TagDto[] = [];

  constructor(
    private listsClient: TodoListsClient,
    private itemsClient: TodoItemsClient,
    private tagsClient: TagsClient,
    private modalService: BsModalService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.listsClient.get().subscribe(
      (result) => {
        this.lists = result.lists;
        this.priorityLevels = result.priorityLevels;
        if (this.lists.length) {
          this.selectedList = this.lists[0];
        }
      },
      (error) => console.error(error)
    );

    // Load tags
    this.loadTags();
  }

  // Lists
  remainingItems(list: TodoListDto): number {
    return list.items.filter((t) => !t.done).length;
  }

  showNewListModal(template: TemplateRef<any>): void {
    this.newListModalRef = this.modalService.show(template);
    setTimeout(() => document.getElementById('title').focus(), 250);
  }

  newListCancelled(): void {
    this.newListModalRef.hide();
    this.newListEditor = {};
  }

  addList(): void {
    const list = {
      id: 0,
      title: this.newListEditor.title,
      items: [],
    } as TodoListDto;

    this.listsClient.create(list as CreateTodoListCommand).subscribe(
      (result) => {
        list.id = result;
        this.lists.push(list);
        this.selectedList = list;
        this.newListModalRef.hide();
        this.newListEditor = {};
      },
      (error) => {
        const errors = JSON.parse(error.response);

        if (errors && errors.Title) {
          this.newListEditor.error = errors.Title[0];
        }

        setTimeout(() => document.getElementById('title').focus(), 250);
      }
    );
  }

  showListOptionsModal(template: TemplateRef<any>) {
    this.listOptionsEditor = {
      id: this.selectedList.id,
      title: this.selectedList.title,
    };

    this.listOptionsModalRef = this.modalService.show(template);
  }

  updateListOptions() {
    const list = this.listOptionsEditor as UpdateTodoListCommand;
    this.listsClient.update(this.selectedList.id, list).subscribe(
      () => {
        (this.selectedList.title = this.listOptionsEditor.title),
          this.listOptionsModalRef.hide();
        this.listOptionsEditor = {};
      },
      (error) => console.error(error)
    );
  }

  confirmDeleteList(template: TemplateRef<any>) {
    this.listOptionsModalRef.hide();
    this.deleteListModalRef = this.modalService.show(template);
  }

  deleteListConfirmed(): void {
    this.listsClient.delete(this.selectedList.id).subscribe(
      () => {
        this.deleteListModalRef.hide();
        this.lists = this.lists.filter((t) => t.id !== this.selectedList.id);
        this.selectedList = this.lists.length ? this.lists[0] : null;
      },
      (error) => console.error(error)
    );
  }

  // Items
  showItemDetailsModal(template: TemplateRef<any>, item: TodoItemDto): void {
    this.selectedItem = item;

    // Prepare form data including tags
    const formData = {
      ...this.selectedItem,
      tags: this.selectedItem.tags
        ? this.selectedItem.tags.map((tag) => tag.id)
        : [],
    };

    this.itemDetailsFormGroup.patchValue(formData);

    this.itemDetailsModalRef = this.modalService.show(template);
    this.itemDetailsModalRef.onHidden.subscribe(() => {
      this.stopDeleteCountDown();
    });
  }

  updateItemDetails(): void {
    const formValue = this.itemDetailsFormGroup.value;
    const item = new UpdateTodoItemDetailCommand({
      id: formValue.id,
      listId: formValue.listId,
      priority: formValue.priority,
      note: formValue.note,
      backgroundColour: formValue.backgroundColour,
    });

    this.itemsClient.updateItemDetails(this.selectedItem.id, item).subscribe(
      () => {
        if (this.selectedItem.listId !== item.listId) {
          this.selectedList.items = this.selectedList.items.filter(
            (i) => i.id !== this.selectedItem.id
          );
          const listIndex = this.lists.findIndex((l) => l.id === item.listId);
          this.selectedItem.listId = item.listId;
          this.lists[listIndex].items.push(this.selectedItem);
        }

        this.selectedItem.priority = item.priority;
        this.selectedItem.note = item.note;
        this.selectedItem.backgroundColour = item.backgroundColour;

        this.itemDetailsModalRef.hide();
        this.itemDetailsFormGroup.reset();
      },
      (error) => console.error(error)
    );
  }

  addItem() {
    const item = {
      id: 0,
      listId: this.selectedList.id,
      priority: this.priorityLevels[0].value,
      title: '',
      done: false,
      backgroundColour: this.supportedColors[0].value,
    } as TodoItemDto;

    this.selectedList.items.push(item);
    const index = this.selectedList.items.length - 1;
    this.editItem(item, 'itemTitle' + index);
  }

  editItem(item: TodoItemDto, inputId: string): void {
    this.selectedItem = item;
    setTimeout(() => document.getElementById(inputId).focus(), 100);
  }

  updateItem(item: TodoItemDto, pressedEnter: boolean = false): void {
    const isNewItem = item.id === 0;

    if (!item.title.trim()) {
      this.deleteItem(item);
      return;
    }

    if (item.id === 0) {
      this.itemsClient
        .create({
          ...item,
          listId: this.selectedList.id,
        } as CreateTodoItemCommand)
        .subscribe(
          (result) => {
            item.id = result;
          },
          (error) => console.error(error)
        );
    } else {
      this.itemsClient.update(item.id, item).subscribe(
        () => console.log('Update succeeded.'),
        (error) => console.error(error)
      );
    }

    this.selectedItem = null;

    if (isNewItem && pressedEnter) {
      setTimeout(() => this.addItem(), 250);
    }
  }

  deleteItem(item: TodoItemDto, countDown?: boolean) {
    if (countDown) {
      if (this.deleting) {
        this.stopDeleteCountDown();
        return;
      }
      this.deleteCountDown = 3;
      this.deleting = true;
      this.deleteCountDownInterval = setInterval(() => {
        if (this.deleting && --this.deleteCountDown <= 0) {
          this.deleteItem(item, false);
        }
      }, 1000);
      return;
    }
    this.deleting = false;
    if (this.itemDetailsModalRef) {
      this.itemDetailsModalRef.hide();
    }

    if (item.id === 0) {
      const itemIndex = this.selectedList.items.indexOf(this.selectedItem);
      this.selectedList.items.splice(itemIndex, 1);
    } else {
      this.itemsClient.delete(item.id).subscribe(
        () =>
          (this.selectedList.items = this.selectedList.items.filter(
            (t) => t.id !== item.id
          )),
        (error) => console.error(error)
      );
    }
  }

  stopDeleteCountDown() {
    clearInterval(this.deleteCountDownInterval);
    this.deleteCountDown = 0;
    this.deleting = false;
  }

  // Tag-related methods
  createTag(): void {
    if (!this.newTagName.trim() || this.isCreatingTag) {
      return;
    }

    this.isCreatingTag = true;

    const tag = {
      id: 0,
      name: this.newTagName.trim(),
    } as TagDto;

    this.tagsClient.create(tag as CreateTagCommand).subscribe(
      (result) => {
        tag.id = result;
        this.tags.push(tag);
        this.filteredTags = [...this.tags]; // Update filtered tags
        this.newTagName = ''; // Clear the input
        this.isCreatingTag = false;
      },
      (error) => {
        console.error('Error creating tag:', error);
        this.isCreatingTag = false;
        // You can add error handling here if needed
      }
    );
  }

  loadTags(): void {
    this.tagsClient.get(null).subscribe(
      (result) => {
        this.tags = result;
        this.filteredTags = [...this.tags];
        // Clear any filter tags that no longer exist
        this.selectedFilterTags = this.selectedFilterTags.filter((filterTag) =>
          this.tags.some((tag) => tag.id === filterTag.id)
        );
      },
      (error) => console.error('Error loading tags:', error)
    );
  }

  // Tag editing methods
  editTag(tag: TagDto, inputId: string): void {
    this.selectedTag = tag;
    setTimeout(() => document.getElementById(inputId).focus(), 100);
  }

  updateTag(tag: TagDto, pressedEnter: boolean = false): void {
    const originalTag = this.tags.find((t) => t.id === tag.id);

    if (!tag.name?.trim()) {
      this.selectedTag = null;
      return;
    }

    console.log(tag);
    this.updatingTagId = tag.id;
    const originalName = originalTag?.name;

    const updateCommand = {
      id: tag.id,
      name: tag.name.trim(),
    } as UpdateTagCommand;

    this.tagsClient.update(tag.id, updateCommand).subscribe(
      () => {
        if (originalTag) {
          originalTag.name = tag.name.trim();
        }
        // Update tag references in all todo items
        this.updateTagReferencesInItems(tag.id, tag.name.trim());
        this.selectedTag = null;
        this.updatingTagId = null;
      },
      (error) => {
        console.error('Error updating tag:', error);
        if (originalTag && originalName) {
          originalTag.name = originalName; // Revert on error
          tag.name = originalName; // Also revert the editing tag
        }
        this.selectedTag = null;
        this.updatingTagId = null;
      }
    );
  }

  deleteTag(tag: TagDto): void {
    this.updatingTagId = tag.id;

    this.tagsClient.delete(tag.id).subscribe(
      () => {
        // Remove the tag from the array
        this.tags = this.tags.filter((t) => t.id !== tag.id);
        this.filteredTags = [...this.tags]; // Update filtered tags
        // Remove tag references from all todo items
        this.removeTagReferencesFromItems(tag.id);
        this.updatingTagId = null;
      },
      (error) => {
        console.error('Error deleting tag:', error);
        this.updatingTagId = null;
      }
    );
  }

  // Helper method to update tag references in all todo items
  private updateTagReferencesInItems(tagId: number, newName: string): void {
    this.lists.forEach((list) => {
      list.items.forEach((item) => {
        if (item.tags) {
          const tagIndex = item.tags.findIndex((t) => t.id === tagId);
          if (tagIndex >= 0) {
            item.tags[tagIndex].name = newName;
          }
        }
      });
    });
  }

  // Helper method to remove tag references from all todo items
  private removeTagReferencesFromItems(tagId: number): void {
    this.lists.forEach((list) => {
      list.items.forEach((item) => {
        if (item.tags) {
          item.tags = item.tags.filter((t) => t.id !== tagId);
        }
      });
    });
  }

  // Simple tag assignment method
  assignTag(tagId: number, todoItemId: number): void {
    const command = new AssignTagsToTodoItemCommand({
      todoItemId: todoItemId,
      tagIds: [tagId],
    });

    this.itemsClient.assignTags(command).subscribe(
      (result) => {
        if (result) {
          // Toggle the tag assignment locally
          const tag = this.tags.find((t) => t.id === tagId);
          if (tag) {
            if (
              this.selectedItem.tags &&
              this.selectedItem.tags.some((t) => t.id === tagId)
            ) {
              // Remove tag
              this.selectedItem.tags = this.selectedItem.tags.filter(
                (t) => t.id !== tagId
              );
            } else {
              // Add tag
              if (!this.selectedItem.tags) {
                this.selectedItem.tags = [];
              }
              this.selectedItem.tags.push(tag);
            }
          }
        }
      },
      (error) => console.error('Error assigning tag:', error)
    );
  }

  // Check if tag is assigned to item
  isTagAssignedToItem(todoItem: TodoItemDto, tag: TagDto): boolean {
    return todoItem.tags && todoItem.tags.some((t) => t.id === tag.id);
  }

  // Filter tags based on search text
  filterTags(): void {
    if (!this.tagSearchText.trim()) {
      this.filteredTags = [...this.tags];
    } else {
      this.filteredTags = this.tags.filter((tag) =>
        tag.name.toLowerCase().includes(this.tagSearchText.toLowerCase())
      );
    }
  }

  // Tag filtering methods
  getAvailableTagsInList(): TagDto[] {
    if (!this.selectedList) return [];

    const allTagsInList = new Set<number>();
    this.selectedList.items.forEach((item) => {
      if (item.tags) {
        item.tags.forEach((tag) => allTagsInList.add(tag.id));
      }
    });

    return this.tags.filter((tag) => allTagsInList.has(tag.id));
  }

  getMostUsedTags(): TagDto[] {
    if (!this.selectedList) return [];

    const tagUsage = new Map<number, number>();

    // Count tag usage across all items in the selected list
    this.selectedList.items.forEach((item) => {
      if (item.tags) {
        item.tags.forEach((tag) => {
          tagUsage.set(tag.id, (tagUsage.get(tag.id) || 0) + 1);
        });
      }
    });

    // Return top 5 most used tags
    return this.tags
      .filter((tag) => tagUsage.has(tag.id))
      .sort((a, b) => (tagUsage.get(b.id) || 0) - (tagUsage.get(a.id) || 0))
      .slice(0, 5);
  }

  isTagInFilter(tag: TagDto): boolean {
    return this.selectedFilterTags.some((t) => t.id === tag.id);
  }

  toggleTagFilter(tag: TagDto): void {
    const index = this.selectedFilterTags.findIndex((t) => t.id === tag.id);
    if (index >= 0) {
      this.selectedFilterTags.splice(index, 1);
    } else {
      this.selectedFilterTags.push(tag);
    }
  }

  clearTagFilters(): void {
    this.selectedFilterTags = [];
  }

  getFilteredItems(): TodoItemDto[] {
    if (!this.selectedList) return [];

    if (this.selectedFilterTags.length === 0) {
      return this.selectedList.items;
    }

    return this.selectedList.items.filter((item) => {
      if (!item.tags || item.tags.length === 0) {
        return false;
      }

      // Item must have ALL selected filter tags
      return this.selectedFilterTags.every((filterTag) =>
        item.tags.some((itemTag) => itemTag.id === filterTag.id)
      );
    });
  }
}
